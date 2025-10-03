using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ASB.Pusher;

public class FunctionPushToTopicAsb
{
    private readonly ILogger _logger;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly Random _random;
    private readonly string[] _randomTexts = 
    {
        "Hello from Azure Functions!",
        "Processing batch data...",
        "System health check completed",
        "Random message generated at {0}",
        "Service Bus test message",
        "Automated workflow triggered",
        "Data synchronization in progress",
        "Background job executed successfully",
        "Timer function running smoothly",
        "Azure Service Bus integration working"
    };

    public FunctionPushToTopicAsb(ILoggerFactory loggerFactory, ServiceBusClient serviceBusClient)
    {
        _logger = loggerFactory.CreateLogger<FunctionPushToTopicAsb>();
        _random = new Random();

        // Používáme Managed Identity pro autentizaci
        _serviceBusClient = AuthHelper.GetServiceBusClient();
    }


    private static string ExtractNamespaceFromConnectionString(string connectionString)
    {
        // Extrahuje namespace URI z connection stringu
        // Např. "Endpoint=sb://namespace.servicebus.windows.net/;Authentication=Managed Identity"
        var endpointStart = connectionString.IndexOf("Endpoint=sb://") + 14;
        var endpointEnd = connectionString.IndexOf("/;", endpointStart);

        if (endpointEnd == -1)
            endpointEnd = connectionString.Length;

        return connectionString.Substring(endpointStart, endpointEnd - endpointStart);
    }

    [Function("FunctionPushToTopicAsb")]
    public async Task Run([TimerTrigger("* * * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);
        
        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }

        await SendRandomMessageToServiceBus();
    }


    private async Task SendRandomMessageToServiceBus()
    {
        try
        {
            // Get queue name from environment variable, default to "messages" if not set
            var queueName = Environment.GetEnvironmentVariable("ServiceBusTopicName") ?? "messages";
            
            // Create a sender for the queue
            await using var sender = _serviceBusClient.CreateSender(queueName);
            
            // Generate random message
            var randomText = DateTime.Now.ToString() + "          ----          " + _randomTexts[_random.Next(_randomTexts.Length)];
            var messageContent = string.Format(randomText, DateTime.Now);
            
            // Create message with additional metadata
            var messageBody = new
            {
                Content = messageContent,
                Timestamp = DateTime.UtcNow,
                Source = "FunctionPushToAsb",
                MessageId = Guid.NewGuid().ToString()
            };
            
            var jsonMessage = JsonSerializer.Serialize(messageBody);
            var serviceBusMessage = new ServiceBusMessage(jsonMessage)
            {
                MessageId = messageBody.MessageId,
                ContentType = "application/json"
            };
            
            // Add custom properties
            serviceBusMessage.ApplicationProperties.Add("Source", "TimerFunction");
            serviceBusMessage.ApplicationProperties.Add("ExecutionTime", DateTime.UtcNow);
            
            // Send the message
            await sender.SendMessageAsync(serviceBusMessage);
            
            _logger.LogInformation("Successfully sent message to Service Bus queue '{queueName}': {message}", 
                queueName, messageContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Service Bus: {errorMessage}", ex.Message);
            throw;
        }
    }
}