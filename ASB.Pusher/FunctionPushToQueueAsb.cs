using Azure.Identity;
using Azure.Messaging.ServiceBus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace ASB.Pusher;

public class FunctionPushToQueueAsb
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

    public FunctionPushToQueueAsb(ILoggerFactory loggerFactory)
    {
        _logger = loggerFactory.CreateLogger<FunctionPushToQueueAsb>();
        _random = new Random();

        // Get connection string from environment variable
        var serviceBusConnectionString = Environment.GetEnvironmentVariable("ServiceBusConnectionString");
        
        if (string.IsNullOrEmpty(serviceBusConnectionString))
        {
            throw new InvalidOperationException("ServiceBusConnectionString environment variable is required");
        }

        // Check if using Managed Identity
        if (serviceBusConnectionString.Contains("Authentication=Managed Identity", StringComparison.OrdinalIgnoreCase))
        {
            var namespaceUri = ExtractNamespaceFromConnectionString(serviceBusConnectionString);
            _logger.LogInformation("Using Managed Identity authentication for Service Bus namespace: {NamespaceUri}", namespaceUri);
            _serviceBusClient = new ServiceBusClient(namespaceUri, new DefaultAzureCredential());
        }
        else
        {
            // Use connection string directly for Shared Access Key authentication
            _logger.LogInformation("Using Shared Access Key authentication for Service Bus");
            _serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        }
    }

    private static string ExtractNamespaceFromConnectionString(string connectionString)
    {
        // Extract namespace URI from connection string
        // Expected format: "Endpoint=sb://namespace.servicebus.windows.net/;Authentication=Managed Identity"
        try
        {
            var endpointStart = connectionString.IndexOf("Endpoint=sb://", StringComparison.OrdinalIgnoreCase) + 14;
            var endpointEnd = connectionString.IndexOf("/;", endpointStart);

            if (endpointEnd == -1)
            {
                // Handle case where there's no trailing "/;" (malformed but possible)
                endpointEnd = connectionString.Length;
                if (connectionString.EndsWith("/"))
                {
                    endpointEnd--;
                }
            }

            var namespaceUri = connectionString.Substring(endpointStart, endpointEnd - endpointStart);
            
            // Validate that we have a proper Service Bus namespace
            if (!namespaceUri.Contains(".servicebus.windows.net"))
            {
                throw new ArgumentException($"Invalid Service Bus namespace URI extracted: {namespaceUri}");
            }

            return namespaceUri;
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Failed to extract namespace from connection string: {connectionString}", ex);
        }
    }

    [Function("FunctionPushToQueueAsb")]
    public async Task Run([TimerTrigger("0 */1 * * * *")] TimerInfo myTimer)
    {
        _logger.LogInformation("C# Timer trigger function executed at: {executionTime}", DateTime.Now);
        
        if (myTimer.ScheduleStatus is not null)
        {
            _logger.LogInformation("Next timer schedule at: {nextSchedule}", myTimer.ScheduleStatus.Next);
        }

        await SendRandomMessageToQueue();
    }

    private async Task SendRandomMessageToQueue()
    {
        try
        {
            // Get queue name from environment variable ASB_QUEUE
            var queueName = Environment.GetEnvironmentVariable("ServiceBusQueueName");
            
            if (string.IsNullOrEmpty(queueName))
            {
                throw new InvalidOperationException("ServiceBusQueueName environment variable is required");
            }
            
            // Create a sender for the queue
            await using var sender = _serviceBusClient.CreateSender(queueName);
            
            // Generate random message
            var randomText = _randomTexts[_random.Next(_randomTexts.Length)];
            var messageContent = string.Format(randomText, DateTime.Now);
            
            // Create message with additional metadata
            var messageBody = new
            {
                Content = messageContent,
                Timestamp = DateTime.UtcNow,
                Source = "FunctionPushToQueueAsb",
                MessageId = Guid.NewGuid().ToString(),
                QueueName = queueName,
                ExecutionId = Guid.NewGuid().ToString(),
                MachineName = Environment.MachineName
            };
            
            var jsonMessage = JsonSerializer.Serialize(messageBody, new JsonSerializerOptions
            {
                WriteIndented = false
            });
            
            var serviceBusMessage = new ServiceBusMessage(jsonMessage)
            {
                MessageId = messageBody.MessageId,
                ContentType = "application/json"
            };
            
            // Add custom properties for queue processing
            serviceBusMessage.ApplicationProperties.Add("Source", "QueueTimerFunction");
            serviceBusMessage.ApplicationProperties.Add("ExecutionTime", DateTime.UtcNow);
            serviceBusMessage.ApplicationProperties.Add("QueueName", queueName);
            serviceBusMessage.ApplicationProperties.Add("MessageType", "RandomText");
            serviceBusMessage.ApplicationProperties.Add("FunctionApp", Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME") ?? "Local");
            
            // Send the message to queue
            await sender.SendMessageAsync(serviceBusMessage);
            
            _logger.LogInformation("Successfully sent message to Service Bus queue '{queueName}': {message}", 
                queueName, messageContent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send message to Service Bus queue: {errorMessage}", ex.Message);
            throw;
        }
    }
}