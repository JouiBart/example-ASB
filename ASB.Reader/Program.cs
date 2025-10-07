using ASB.Reader.Helpers;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ASB.Reader;

public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("🚀 Azure Service Bus Reader Starting...");
        Console.WriteLine(new string('=', 60));

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

        try
        {
            // Get user choice for entity type
            var (entityType, entityName, subscriptionName) = GetUserEntityChoice(configuration);
            
            // Get the message reader service
            var messageReader = host.Services.GetRequiredService<ServiceBusMessageReader>();
            
            // Handle Ctrl+C gracefully
            using var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                e.Cancel = true;
                cancellationTokenSource.Cancel();
                Console.WriteLine("\n🛑 Shutdown requested...");
            };

            // Start reading messages with user-selected configuration
            await messageReader.StartReadingAsync(entityType, entityName, subscriptionName, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("✋ Application stopped by user.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Application error: {ex.Message}");
            Console.WriteLine($"🔍 Details: {ex}");
        }
        finally
        {
            await host.StopAsync();
            host.Dispose();
            Console.WriteLine("👋 Goodbye!");
        }
    }

    private static (ServiceBusEntityType entityType, string entityName, string? subscriptionName) GetUserEntityChoice(IConfiguration configuration)
    {
        Console.WriteLine("📋 Vyberte typ Service Bus entity:");
        Console.WriteLine("1️⃣  [Q] Queue - Zpracování zpráv z fronty");
        Console.WriteLine("2️⃣  [T] Topic - Zpracování zpráv z tématu (vyžaduje subscription)");
        Console.WriteLine();

        ServiceBusEntityType entityType;
        while (true)
        {
            Console.Write("👉 Zadejte volbu (Q/T): ");
            var choice = Console.ReadLine()?.Trim().ToUpperInvariant();

            if (choice == "Q" || choice == "1")
            {
                entityType = ServiceBusEntityType.Queue;
                break;
            }
            else if (choice == "T" || choice == "2")
            {
                entityType = ServiceBusEntityType.Topic;
                break;
            }
            else
            {
                Console.WriteLine("❌ Neplatná volba. Zadejte prosím Q pro Queue nebo T pro Topic.");
            }
        }

        string entityName;
        string? subscriptionName = null;

        if (entityType == ServiceBusEntityType.Queue)
        {
            Console.WriteLine();
            Console.WriteLine("🗂️  KONFIGURACE QUEUE");
            var defaultQueue = configuration["ServiceBusQueueName"] ?? "asb-queue-test";
            Console.Write($"📝 Zadejte název Queue (default: {defaultQueue}): ");
            var userInput = Console.ReadLine()?.Trim();
            entityName = string.IsNullOrEmpty(userInput) ? defaultQueue : userInput;
        }
        else
        {
            Console.WriteLine();
            Console.WriteLine("📡 KONFIGURACE TOPIC");
            var defaultTopic = configuration["ServiceBusTopicName"] ?? "asb-topic-test";
            Console.Write($"📝 Zadejte název Topic (default: {defaultTopic}): ");
            var userInput = Console.ReadLine()?.Trim();
            entityName = string.IsNullOrEmpty(userInput) ? defaultTopic : userInput;

            while (string.IsNullOrWhiteSpace(subscriptionName))
            {
                Console.Write("📮 Zadejte název Subscription (default: asb-topic-test-subscription): ");
                userInput = Console.ReadLine()?.Trim();
                subscriptionName = string.IsNullOrEmpty(userInput) ? "asb-topic-test-subscription" : userInput;
                if (string.IsNullOrWhiteSpace(subscriptionName))
                {
                    Console.WriteLine("❌ Název subscription je povinný pro Topic!");
                }
            }
        }

        Console.WriteLine();
        Console.WriteLine(new string('-', 60));
        Console.WriteLine($"✅ KONFIGUROVÁNO:");
        Console.WriteLine($"   Typ: {entityType}");
        Console.WriteLine($"   Název: {entityName}");
        if (subscriptionName != null)
            Console.WriteLine($"   Subscription: {subscriptionName}");
        Console.WriteLine(new string('-', 60));
        Console.WriteLine();

        return (entityType, entityName, subscriptionName);
    }
}


