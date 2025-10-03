using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Azure.Messaging.ServiceBus;
using Azure.Identity;
using System.Text.Json;

namespace FunctionApp.Functions;

public class ExampleTestTopicFunction
{
    private readonly ILogger _logger;
    private readonly ServiceBusClient _serviceBusClient;

    public ExampleTestTopicFunction(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<ExampleTestTopicFunction>();
        
        var serviceBusNamespace = Environment.GetEnvironmentVariable("ServiceBusConnection__fullyQualifiedNamespace");
        
        if (string.IsNullOrEmpty(serviceBusNamespace))
        {
            throw new InvalidOperationException("ServiceBusConnection__fullyQualifiedNamespace environment variable is not set");
        }
        
        _serviceBusClient = new ServiceBusClient(serviceBusNamespace, new DefaultAzureCredential());
    }

    [Function("SendToExampleTestTopic")]
    public async Task<HttpResponseData> SendToExampleTestTopic([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequestData req)
    {
        _logger.LogInformation("Sending message to example-test topic");

        try
        {
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            
            if (string.IsNullOrEmpty(requestBody))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Request body is empty");
                return badResponse;
            }

            var messageRequest = JsonSerializer.Deserialize<ExampleTestMessage>(requestBody);
            
            if (messageRequest == null || string.IsNullOrEmpty(messageRequest.Content))
            {
                var badResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await badResponse.WriteStringAsync("Content is required");
                return badResponse;
            }

            // Příprava zprávy pro topic
            var topicName = Environment.GetEnvironmentVariable("ServiceBusTopicName") ?? "example-test";
            var messageId = Guid.NewGuid().ToString();

            var serviceBusMessage = new ServiceBusMessage(JsonSerializer.Serialize(messageRequest))
            {
                MessageId = messageId,
                ContentType = "application/json",
                TimeToLive = TimeSpan.FromHours(24),
                Subject = messageRequest.Category ?? "general" // Subject pro filtering
            };

            // Custom properties pro subscription filters
            serviceBusMessage.ApplicationProperties["Category"] = messageRequest.Category ?? "general";
            serviceBusMessage.ApplicationProperties["Priority"] = messageRequest.Priority ?? "normal";
            serviceBusMessage.ApplicationProperties["Source"] = "dotnet-function";
            serviceBusMessage.ApplicationProperties["Timestamp"] = DateTime.UtcNow.ToString("O");

            // Odeslání do topic
            var sender = _serviceBusClient.CreateSender(topicName);
            await sender.SendMessageAsync(serviceBusMessage);
            await sender.CloseAsync();

            _logger.LogInformation("Message sent to example-test topic: {MessageId}, Category: {Category}", 
                messageId, messageRequest.Category);

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");

            var responseObject = new
            {
                Success = true,
                MessageId = messageId,
                TopicName = topicName,
                Category = messageRequest.Category,
                Timestamp = DateTime.UtcNow.ToString("O")
            };

            await response.WriteStringAsync(JsonSerializer.Serialize(responseObject));
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message to example-test topic: {Error}", ex.Message);
            
            var errorResponse = req.CreateResponse(HttpStatusCode.InternalServerError);
            await errorResponse.WriteStringAsync("Internal server error");
            return errorResponse;
        }
    }

    [Function("ProcessExampleTestMessage")]
    public async Task ProcessExampleTestMessage(
        [ServiceBusTrigger("example-test", "example-test-subscription", Connection = "ServiceBusConnection")] 
        ServiceBusReceivedMessage message,
        FunctionContext context)
    {
        _logger.LogInformation("Processing example-test topic message: MessageId={MessageId}, Subject={Subject}", 
            message.MessageId, message.Subject);

        try
        {
            var messageContent = message.Body.ToString();
            var exampleTestMessage = JsonSerializer.Deserialize<ExampleTestMessage>(messageContent);

            if (exampleTestMessage == null)
            {
                throw new InvalidOperationException("Failed to deserialize message content");
            }

            // Získání properties z zprávy
            var category = message.ApplicationProperties.TryGetValue("Category", out var categoryValue) 
                ? categoryValue?.ToString() ?? "unknown" : "unknown";
            var priority = message.ApplicationProperties.TryGetValue("Priority", out var priorityValue) 
                ? priorityValue?.ToString() ?? "normal" : "normal";

            _logger.LogInformation("Processing example-test message - Category: {Category}, Priority: {Priority}, Content: {Content}", 
                category, priority, exampleTestMessage.Content);

            // Business logika na základě kategorie
            await ProcessByCategory(exampleTestMessage, category, priority, context);

            _logger.LogInformation("Example-test message processed successfully: MessageId={MessageId}", message.MessageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing example-test message: {Error}", ex.Message);
            throw; // Re-throw pro retry mechanismus
        }
    }

    private async Task ProcessByCategory(ExampleTestMessage message, string category, string priority, FunctionContext context)
    {
        _logger.LogInformation("Processing category: {Category} with priority: {Priority}", category, priority);

        switch (category.ToLower())
        {
            case "notification":
                await ProcessNotification(message, priority);
                break;
                
            case "analytics":
                await ProcessAnalytics(message, priority);
                break;
                
            case "audit":
                await ProcessAudit(message, priority);
                break;
                
            default:
                await ProcessGeneral(message, priority);
                break;
        }
    }

    private async Task ProcessNotification(ExampleTestMessage message, string priority)
    {
        _logger.LogInformation("Processing notification: {Content}", message.Content);
        
        // Simulace notification processing
        var delay = priority == "high" ? 50 : 200;
        await Task.Delay(delay);
        
        // Zde by bylo volání notification service, email, SMS, etc.
    }

    private async Task ProcessAnalytics(ExampleTestMessage message, string priority)
    {
        _logger.LogInformation("Processing analytics data: {Content}", message.Content);
        
        // Simulace analytics processing
        var delay = priority == "high" ? 100 : 500;
        await Task.Delay(delay);
        
        // Zde by bylo ukládání do analytics databáze, data lake, etc.
    }

    private async Task ProcessAudit(ExampleTestMessage message, string priority)
    {
        _logger.LogInformation("Processing audit log: {Content}", message.Content);
        
        // Audit logs mají vždy vysokou prioritu
        await Task.Delay(50);
        
        // Zde by bylo ukládání do audit systému
    }

    private async Task ProcessGeneral(ExampleTestMessage message, string priority)
    {
        _logger.LogInformation("Processing general message: {Content}", message.Content);
        
        // General processing
        var delay = priority == "high" ? 150 : 300;
        await Task.Delay(delay);
    }
}

// Data model pro example-test zprávy
public class ExampleTestMessage
{
    public string? Content { get; set; }
    public string? Category { get; set; } // notification, analytics, audit, general
    public string? Priority { get; set; } // high, normal, low
    public Dictionary<string, object>? Metadata { get; set; }
}