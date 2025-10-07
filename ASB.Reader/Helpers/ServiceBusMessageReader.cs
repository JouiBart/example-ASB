using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace ASB.Reader.Helpers
{
    public enum ServiceBusEntityType
    {
        Queue,
        Topic
    }

    public enum MessageAction
    {
        Complete,
        Abandon,
        DeadLetter,
        Skip
    }

    public class ServiceBusMessageReader
    {
        private readonly ServiceBusClient _serviceBusClient;
        private readonly ILogger<ServiceBusMessageReader> _logger;
        private readonly IConfiguration _configuration;
        private ServiceBusProcessor? _processor;
        private readonly SemaphoreSlim _messageProcessingSemaphore = new(1, 1);

        public ServiceBusMessageReader(
            ServiceBusClient serviceBusClient,
            ILogger<ServiceBusMessageReader> logger,
            IConfiguration configuration)
        {
            _serviceBusClient = serviceBusClient;
            _logger = logger;
            _configuration = configuration;
        }

        public async Task StartReadingAsync(ServiceBusEntityType entityType, string entityName, string? subscriptionName = null, CancellationToken cancellationToken = default)
        {
            var subscriptionInfo = subscriptionName != null ? $" (subscription: {subscriptionName})" : "";
            _logger.LogInformation("Starting to read messages from {EntityType}: {EntityName}{Subscription}", 
                entityType, entityName, subscriptionInfo);

            // Create appropriate processor
            _processor = entityType == ServiceBusEntityType.Queue
                ? _serviceBusClient.CreateProcessor(entityName, new ServiceBusProcessorOptions
                {
                    MaxConcurrentCalls = 1,
                    AutoCompleteMessages = false,
                    ReceiveMode = ServiceBusReceiveMode.PeekLock
                })
                : _serviceBusClient.CreateProcessor(entityName, subscriptionName!, new ServiceBusProcessorOptions
                {
                    MaxConcurrentCalls = 1,
                    AutoCompleteMessages = false,
                    ReceiveMode = ServiceBusReceiveMode.PeekLock
                });

            // Add handlers for processing messages and handling errors
            _processor.ProcessMessageAsync += MessageHandler;
            _processor.ProcessErrorAsync += ErrorHandler;

            // Start processing messages
            await _processor.StartProcessingAsync(cancellationToken);

            Console.WriteLine($"\n✅ Message processor started for {entityType}: {entityName}{(subscriptionName != null ? $"/{subscriptionName}" : "")}");
            Console.WriteLine("📨 Waiting for messages... Press Ctrl+C to stop.\n");

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
            // Zajistíme, že zpracováváme zprávy sekvenčně
            await _messageProcessingSemaphore.WaitAsync();
            
            try
            {
                var messageId = args.Message.MessageId;
                var body = args.Message.Body.ToString();

                Console.WriteLine(new string('=', 80));
                Console.WriteLine($"📨 NOVÁ ZPRÁVA PŘIJATA");
                Console.WriteLine($"🆔 Message ID: {messageId}");
                Console.WriteLine($"⏰ Enqueued Time: {args.Message.EnqueuedTime:yyyy-MM-dd HH:mm:ss} UTC");
                Console.WriteLine($"🔄 Delivery Count: {args.Message.DeliveryCount}");
                Console.WriteLine($"📝 Content Type: {args.Message.ContentType}");

                // Display message properties
                if (args.Message.ApplicationProperties.Count > 0)
                {
                    Console.WriteLine("🏷️ Message Properties:");
                    foreach (var property in args.Message.ApplicationProperties)
                    {
                        Console.WriteLine($"    {property.Key}: {property.Value}");
                    }
                }

                Console.WriteLine("\n📄 MESSAGE CONTENT:");
                Console.WriteLine(new string('-', 40));
                
                // Try to deserialize as JSON if possible
                try
                {
                    var jsonDocument = JsonDocument.Parse(body);
                    var formattedJson = JsonSerializer.Serialize(jsonDocument, new JsonSerializerOptions
                    {
                        WriteIndented = true
                    });
                    Console.WriteLine(formattedJson);
                }
                catch (JsonException)
                {
                    Console.WriteLine(body);
                }

                Console.WriteLine(new string('-', 40));

                // Prompt user for action
                var action = await PromptUserForActionAsync();
                await ExecuteMessageActionAsync(args, action, messageId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing message {MessageId}: {Error}",
                    args.Message.MessageId, ex.Message);

                Console.WriteLine($"❌ Chyba při zpracování zprávy: {ex.Message}");
                Console.WriteLine("📤 Zpráva bude automaticky vrácena do fronty (Abandon).");
                
                try
                {
                    await args.AbandonMessageAsync(args.Message);
                }
                catch (Exception abandonEx)
                {
                    _logger.LogError(abandonEx, "Failed to abandon message after processing error");
                }
            }
            finally
            {
                _messageProcessingSemaphore.Release();
            }
        }

        private async Task<MessageAction> PromptUserForActionAsync()
        {
            while (true)
            {
                Console.WriteLine("\n🤔 Co chcete s touto zprávou udělat?");
                Console.WriteLine("1️⃣  [C] Complete - Označit jako zpracovanou (smazat z fronty)");
                Console.WriteLine("2️⃣  [A] Abandon - Vrátit zpět do fronty");
                Console.WriteLine("3️⃣  [D] Dead Letter - Přesunout do Dead Letter Queue");
                Console.WriteLine("4️⃣  [S] Skip - Pokračovat bez akce (zpráva zůstane v lock stavu)");
                Console.Write("\n👉 Zadejte volbu (C/A/D/S): ");

                var input = Console.ReadLine()?.Trim().ToUpperInvariant();

                return input switch
                {
                    "C" or "1" => MessageAction.Complete,
                    "A" or "2" => MessageAction.Abandon,
                    "D" or "3" => MessageAction.DeadLetter,
                    "S" or "4" => MessageAction.Skip,
                    _ => await HandleInvalidInputAsync()
                };
            }
        }

        private async Task<MessageAction> HandleInvalidInputAsync()
        {
            Console.WriteLine("❌ Neplatná volba. Zadejte prosím C, A, D nebo S.");
            await Task.Delay(1000); // Krátká pauza pro lepší UX
            return await PromptUserForActionAsync();
        }

        private async Task ExecuteMessageActionAsync(ProcessMessageEventArgs args, MessageAction action, string messageId)
        {
            try
            {
                switch (action)
                {
                    case MessageAction.Complete:
                        await args.CompleteMessageAsync(args.Message);
                        Console.WriteLine($"✅ Zpráva {messageId} byla označena jako zpracovaná (Complete).");
                        _logger.LogInformation("Message {MessageId} completed successfully", messageId);
                        break;

                    case MessageAction.Abandon:
                        await args.AbandonMessageAsync(args.Message);
                        Console.WriteLine($"🔄 Zpráva {messageId} byla vrácena do fronty (Abandon).");
                        _logger.LogInformation("Message {MessageId} abandoned", messageId);
                        break;

                    case MessageAction.DeadLetter:
                        await args.DeadLetterMessageAsync(args.Message, "User requested", "Message moved to dead letter queue by user choice");
                        Console.WriteLine($"💀 Zpráva {messageId} byla přesunuta do Dead Letter Queue.");
                        _logger.LogInformation("Message {MessageId} moved to dead letter queue", messageId);
                        break;

                    case MessageAction.Skip:
                        Console.WriteLine($"⏭️  Zpráva {messageId} byla přeskočena (zůstává v lock stavu).");
                        _logger.LogInformation("Message {MessageId} skipped - no action taken", messageId);
                        break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute action {Action} on message {MessageId}", action, messageId);
                Console.WriteLine($"❌ Chyba při provádění akce {action}: {ex.Message}");
            }

            Console.WriteLine(new string('=', 80));
            Console.WriteLine("🔄 Čekám na další zprávu...\n");
        }

        private Task ErrorHandler(ProcessErrorEventArgs args)
        {
            _logger.LogError(args.Exception, "Service Bus error occurred: {Error}", args.Exception.Message);
            _logger.LogError("Error source: {Source}", args.ErrorSource);
            _logger.LogError("Entity path: {EntityPath}", args.EntityPath);

            Console.WriteLine($"❌ Service Bus chyba: {args.Exception.Message}");
            Console.WriteLine($"🔍 Zdroj chyby: {args.ErrorSource}");
            Console.WriteLine($"📍 Entity path: {args.EntityPath}");

            return Task.CompletedTask;
        }
    }
}
