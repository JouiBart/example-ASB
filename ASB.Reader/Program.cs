using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ASB.Reader;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Azure Service Bus Reader Starting...");

        // Build configuration
        var configuration = new ConfigurationBuilder()
            .AddEnvironmentVariables()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        // Build host with dependency injection
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<IConfiguration>(configuration);
                services.AddSingleton<ServiceBusClient>(provider =>
                {
                    var connectionString = configuration["ServiceBusConnectionString"] 
                        ?? Environment.GetEnvironmentVariable("ServiceBusConnectionString")
                        ?? throw new InvalidOperationException("ServiceBusConnectionString is required");
                    return new ServiceBusClient(connectionString);
                });
                services.AddSingleton<ServiceBusMessageReader>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        // Get the message reader service and start reading
        var messageReader = host.Services.GetRequiredService<ServiceBusMessageReader>();
        
        // Handle Ctrl+C gracefully
        using var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
            Console.WriteLine("\nShutdown requested...");
        };

        try
        {
            await messageReader.StartReadingAsync(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Application stopped.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Application error: {ex.Message}");
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
        }
    }
}

public class ServiceBusMessageReader
{
    private readonly ServiceBusClient _serviceBusClient;
    private readonly ILogger<ServiceBusMessageReader> _logger;
    private readonly IConfiguration _configuration;
    private ServiceBusProcessor? _processor;

    public ServiceBusMessageReader(
        ServiceBusClient serviceBusClient, 
        ILogger<ServiceBusMessageReader> logger,
        IConfiguration configuration)
    {
        _serviceBusClient = serviceBusClient;
        _logger = logger;
        _configuration = configuration;
    }

    public async Task StartReadingAsync(CancellationToken cancellationToken = default)
    {
        var queueName = _configuration["ServiceBusQueueName"] 
            ?? Environment.GetEnvironmentVariable("ServiceBusQueueName") 
            ?? "messages";

        _logger.LogInformation("Starting to read messages from queue: {QueueName}", queueName);

        // Create a processor for the queue
        _processor = _serviceBusClient.CreateProcessor(queueName, new ServiceBusProcessorOptions
        {
            MaxConcurrentCalls = 1,
            AutoCompleteMessages = false
        });

        // Add handlers for processing messages and handling errors
        _processor.ProcessMessageAsync += MessageHandler;
        _processor.ProcessErrorAsync += ErrorHandler;

        // Start processing messages
        await _processor.StartProcessingAsync(cancellationToken);

        _logger.LogInformation("Message processor started. Press Ctrl+C to stop.");

        // Keep the application running until cancellation is requested
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Cancellation requested, stopping processor...");
        }
        finally
        {
            if (_processor != null)
            {
                await _processor.StopProcessingAsync();
                await _processor.DisposeAsync();
            }
        }
    }

    private async Task MessageHandler(ProcessMessageEventArgs args)
    {
        try
        {
            var messageId = args.Message.MessageId;
            var body = args.Message.Body.ToString();
            
            _logger.LogInformation("Received message ID: {MessageId}", messageId);
            _logger.LogInformation("Message body: {Body}", body);

            // Try to deserialize as JSON if possible
            try
            {
                var jsonDocument = JsonDocument.Parse(body);
                var formattedJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions 
                { 
                    WriteIndented = true 
                });
                Console.WriteLine("Formatted JSON message:");
                Console.WriteLine(formattedJson);
            }
            catch (JsonException)
            {
                Console.WriteLine("Plain text message:");
                Console.WriteLine(body);
            }

            // Display message properties
            if (args.Message.ApplicationProperties.Count > 0)
            {
                Console.WriteLine("Message properties:");
                foreach (var property in args.Message.ApplicationProperties)
                {
                    Console.WriteLine($"  {property.Key}: {property.Value}");
                }
            }

            // Display message metadata
            Console.WriteLine($"Content Type: {args.Message.ContentType}");
            Console.WriteLine($"Enqueued Time: {args.Message.EnqueuedTime:yyyy-MM-dd HH:mm:ss} UTC");
            Console.WriteLine($"Delivery Count: {args.Message.DeliveryCount}");
            Console.WriteLine(new string('-', 50));

            // Complete the message so it's removed from the queue
            await args.CompleteMessageAsync(args.Message);
            
            _logger.LogInformation("Message {MessageId} processed successfully", messageId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message {MessageId}: {Error}", 
                args.Message.MessageId, ex.Message);
            
            // Abandon the message so it can be reprocessed
            await args.AbandonMessageAsync(args.Message);
        }
    }

    private Task ErrorHandler(ProcessErrorEventArgs args)
    {
        _logger.LogError(args.Exception, "Service Bus error occurred: {Error}", args.Exception.Message);
        _logger.LogError("Error source: {Source}", args.ErrorSource);
        _logger.LogError("Entity path: {EntityPath}", args.EntityPath);
        
        return Task.CompletedTask;
    }
}
