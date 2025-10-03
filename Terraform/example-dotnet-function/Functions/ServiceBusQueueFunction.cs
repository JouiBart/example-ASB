using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Azure.Identity;
using System.Text.Json;

namespace FunctionApp.Functions;

public class ServiceBusQueueFunction
{
    private readonly ILogger _logger;
    private readonly ServiceBusClient _serviceBusClient;

    public ServiceBusQueueFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ServiceBusQueueFunction>();
        
        // Pro Managed Identity používáme pouze namespace, ne connection string
        var serviceBusNamespace = Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace");
        
        if (string.IsNullOrEmpty(serviceBusNamespace))
        {
            throw new InvalidOperationException("ServiceBusConnection__fullyQualifiedNamespace environment variable is not set");
        }
        
        // Používáme Managed Identity pro autentizaci
        _serviceBusClient = new ServiceBusClient(serviceBusNamespace, new DefaultAzureCredential());
    }

    [Function("SendToServiceBusQueue")]
    public async Task<HttpResponseData> SendToServiceBusQueue([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Sending message to Service Bus queue");

        try
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body is empty");
                return badResponse;
            }

            var messageRequest = JsonSerializer.Deserialize<ServiceBusMessage>(requestBody);
            
            if (messageRequest == null || string.IsNullOrEmpty(messageRequest.Content))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Content is required");
                return badResponse;
            }

            // Získání queue name z environment proměnné
            var queueName = Environment.GetEnvironmentVariable("ServiceBusQueueName") ?? "test";
            var messageId = Guid.NewGuid().ToString();

            var serviceBusMessage = new Azure.Messaging.ServiceBus.ServiceBusMessage(JsonSerializer.Serialize(messageRequest))
            {
                MessageId = messageId,
                ContentType = "application/json",
                TimeToLive = TimeSpan.FromHours(1)
            };

            // Custom properties
            serviceBusMessage.ApplicationProperties["Source"] = "dotnet-function";
            serviceBusMessage.ApplicationProperties["Timestamp"] = DateTime.UtcNow.ToString("O");
            if (!string.IsNullOrEmpty(messageRequest.Category))
            {
                serviceBusMessage.ApplicationProperties["Category"] = messageRequest.Category;
            }

            // Odeslání do queue
            var sender = _serviceBusClient.CreateSender(queueName);
            await sender.SendMessageAsync(serviceBusMessage);
            await sender.CloseAsync();

            _logger.LogInformation("Message sent to queue {QueueName}: {MessageId}", queueName, messageId);

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
            _logger.LogError(ex, "Error sending message to Service Bus queue: {Error}", ex.Message);
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    [Function("ProcessServiceBusMessage")]
    public async Task ProcessServiceBusMessage(
        [ServiceBusTrigger("%ServiceBusQueueName%", Connection = "ServiceBusConnection")] 
        ServiceBusReceivedMessage message,
        FunctionContext context)
    {
        _logger.LogInformation("Processing Service Bus queue message: MessageId={MessageId}", message.MessageId);

        try
        {
            var messageContent = message.Body.ToString();
            var serviceBusMessage = JsonSerializer.Deserialize<ServiceBusMessage>(messageContent);

            if (serviceBusMessage == null)
            {
                throw new InvalidOperationException("Failed to deserialize message content");
            }

            // Získání properties z zprávy
            var category = message.ApplicationProperties.TryGetValue("Category", out var categoryValue) 
                ? categoryValue?.ToString() ?? "general" : "general";

            _logger.LogInformation("Processing Service Bus message - Category: {Category}, Content: {Content}", 
                category, serviceBusMessage.Content);

            // Simulace zpracování podle kategorie
            await ProcessMessageByCategory(serviceBusMessage, category, context);

            _logger.LogInformation("Service Bus message processed successfully: MessageId={MessageId}", message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Service Bus message: {Error}", ex.Message);
            throw; // Re-throw pro retry mechanismus
        }
    }

    private async Task ProcessMessageByCategory(ServiceBusMessage message, string category, FunctionContext context)
    {
        _logger.LogInformation("Processing category: {Category}", category);

        switch (category.ToLower())
        {
            case "notification":
                await ProcessNotification(message);
                break;
                
            case "order":
                await ProcessOrder(message);
                break;
                
            case "audit":
                await ProcessAudit(message);
                break;
                
            default:
                await ProcessGeneral(message);
                break;
        }
    }

    private async Task ProcessNotification(ServiceBusMessage message)
    {
        _logger.LogInformation("Processing notification: {Content}", message.Content);
        await Task.Delay(100); // Simulace processing
    }

    private async Task ProcessOrder(ServiceBusMessage message)
    {
        _logger.LogInformation("Processing order: {Content}", message.Content);
        await Task.Delay(200); // Simulace processing
    }

    private async Task ProcessAudit(ServiceBusMessage message)
    {
        _logger.LogInformation("Processing audit log: {Content}", message.Content);
        await Task.Delay(50); // Audit logs jsou důležité - rychlé zpracování
    }

    private async Task ProcessGeneral(ServiceBusMessage message)
    {
        _logger.LogInformation("Processing general message: {Content}", message.Content);
        await Task.Delay(150); // General processing
    }
}

// Data model pro Service Bus zprávy
public class ServiceBusMessage
{
    public string? Content { get; set; }
    public string? Category { get; set; }
    public Dictionary<string, object>? Metadata { get; set; }
}