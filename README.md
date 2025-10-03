# ASB.Reader - Azure Service Bus Message Reader

This console application reads messages from an Azure Service Bus queue and displays them in a formatted way.

## Features

- Continuous message reading from Azure Service Bus queue
- JSON message formatting and pretty-printing
- Message properties and metadata display
- Graceful shutdown with Ctrl+C
- Proper error handling and logging
- Configurable via environment variables or appsettings.json

## Configuration

### Required Settings

- **ServiceBusConnectionString**: Your Azure Service Bus connection string

### Optional Settings

- **ServiceBusQueueName**: Name of the queue to read from (defaults to "messages")

### Configuration Methods

#### 1. Environment Variables
```bash
set ServiceBusConnectionString=Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key
set ServiceBusQueueName=messages
```

#### 2. appsettings.json
Update the `appsettings.json` file with your connection string:
```json
{
  "ServiceBusConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=your-key",
  "ServiceBusQueueName": "messages"
}
```

## Usage

1. Configure your Azure Service Bus connection string
2. Run the application:
   ```bash
   dotnet run
   ```
3. The application will start reading messages from the specified queue
4. Messages will be displayed in the console with formatting
5. Press Ctrl+C to stop the application gracefully

## Message Display Format

The application displays:
- Message ID
- Message body (formatted as JSON if possible, otherwise as plain text)
- Application properties
- Message metadata (Content Type, Enqueued Time, Delivery Count)

## Error Handling

- Messages that fail to process are abandoned and can be reprocessed
- Service Bus errors are logged with detailed information
- Application continues running even if individual messages fail to process