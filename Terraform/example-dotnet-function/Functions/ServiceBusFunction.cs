using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Azure.Identity;
using System.Text.Json;

namespace FunctionApp.Functions;

public class ServiceBusFunction
{
    private readonly ILogger _logger;
    private readonly ServiceBusClient _serviceBusClient;

    public ServiceBusFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ServiceBusFunction>();
        
        // Inicializace Service Bus klienta s Managed Identity
        var serviceBusNamespace = Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace");
        
        if (string.IsNullOrEmpty(serviceBusNamespace))
        {
            throw new InvalidOperationException("ServiceBusConnection__fullyQualifiedNamespace environment variable is not set");
        }
        
        _serviceBusClient = new ServiceBusClient(serviceBusNamespace, new DefaultAzureCredential());
    }

    [Function("SendToServiceBus")]
    public async Task<HttpResponseData> SendToServiceBus([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("C# HTTP trigger function processed a request to send message to Service Bus.");

        try
        {
            // Čtení request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body is empty");
                return badResponse;
            }

            // Deserializace JSON
            MessageRequest? messageRequest;
            try
            {
                messageRequest = JsonSerializer.Deserialize<MessageRequest>(requestBody);
            }
            catch (JsonException ex)
            {
                _logger.LogError("Failed to deserialize request body: {Error}", ex.Message);
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Invalid JSON format");
                return badResponse;
            }

            if (messageRequest == null || string.IsNullOrEmpty(messageRequest.Message))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Message is required");
                return badResponse;
            }

            // Příprava Service Bus zprávy
            var queueName = Environment.GetEnvironmentVariable("MESSAGE_QUEUE_NAME") ?? "messages-queue";
            var messageId = messageRequest.MessageId ?? Guid.NewGuid().ToString();

            var serviceBusMessage = new ServiceBusMessage(messageRequest.Message)
            {
                MessageId = messageId,
                ContentType = "application/json",
                TimeToLive = TimeSpan.FromHours(24)
            };

            // Přidání custom properties
            serviceBusMessage.ApplicationProperties["Source"] = "dotnet-function";
            serviceBusMessage.ApplicationProperties["Timestamp"] = DateTime.UtcNow.ToString("O");
            
            if (!string.IsNullOrEmpty(messageRequest.Priority))
            {
                serviceBusMessage.ApplicationProperties["Priority"] = messageRequest.Priority;
            }

            // Odeslání do Service Bus
            var sender = _serviceBusClient.CreateSender(queueName);
            await sender.SendMessageAsync(serviceBusMessage);
            await sender.CloseAsync();

            _logger.LogInformation("Message sent successfully to Service Bus queue: {QueueName}, MessageId: {MessageId}", 
                queueName, messageId);

            // Odpověď
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var responseObject = new
            {
                Success = true,
                MessageId = messageId,
                QueueName = queueName,
                Timestamp = DateTime.UtcNow.ToString("O")
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(responseObject));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to Service Bus: {Error}", ex.Message);
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    [Function("ProcessServiceBusMessage")]
    public async Task ProcessServiceBusMessage(
        [ServiceBusTrigger("messages-queue", Connection = "ServiceBusConnection")] ServiceBusReceivedMessage message,
        FunctionContext context)
    {
        _logger.LogInformation("Processing Service Bus message: MessageId={MessageId}, DeliveryCount={DeliveryCount}", 
            message.MessageId, message.DeliveryCount);

        try
        {
            var messageBody = message.Body.ToString();
            _logger.LogInformation("Message content: {MessageBody}", messageBody);

            // Získání custom properties
            var source = message.ApplicationProperties.TryGetValue("Source", out var sourceValue) ? sourceValue?.ToString() : "unknown";
            var priority = message.ApplicationProperties.TryGetValue("Priority", out var priorityValue) ? priorityValue?.ToString() : "normal";
            var timestamp = message.ApplicationProperties.TryGetValue("Timestamp", out var timestampValue) ? timestampValue?.ToString() : DateTime.UtcNow.ToString("O");

            _logger.LogInformation("Message properties - Source: {Source}, Priority: {Priority}, Timestamp: {Timestamp}", 
                source, priority, timestamp);

            // Zde by byla vaše business logika
            await ProcessBusinessLogic(messageBody, priority ?? "normal", context);

            _logger.LogInformation("Message processed successfully: MessageId={MessageId}", message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Service Bus message: {Error}", ex.Message);
            
            // Re-throw pro retry mechanismus Service Bus
            throw;
        }
    }

    private async Task ProcessBusinessLogic(string messageBody, string priority, FunctionContext context)
    {
        // Simulace business logiky na základě priority
        var processingTime = priority?.ToLower() switch
        {
            "high" => TimeSpan.FromMilliseconds(100),
            "low" => TimeSpan.FromMilliseconds(500),
            _ => TimeSpan.FromMilliseconds(250)
        };

        _logger.LogInformation("Processing message with priority: {Priority}, estimated time: {ProcessingTime}ms", 
            priority, processingTime.TotalMilliseconds);

        await Task.Delay(processingTime);

        // Zde byste implementovali skutečnou business logiku:
        // - Uložení do databáze
        // - Volání externích API
        // - Transformace dat
        // - Notifikace
    }
}

// Data model pro příchozí zprávy
public class MessageRequest
{
    public string? Message { get; set; }
    public string? MessageId { get; set; }
    public string? Priority { get; set; }
}