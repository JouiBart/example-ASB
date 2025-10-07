# Azure Service Bus Reader & Pusher

Ukázkové aplikace pro práci s Azure Service Bus - ?tení a odesílání zpráv do Queue a Topic.

## ?? Struktura projektu

```
example-ASB/
??? ASB.Reader/          # Konzolová aplikace pro ?tení zpráv (.NET 9)
?   ??? Program.cs
?   ??? Helpers/
?   ?   ??? ServiceBusMessageReader.cs
?   ??? appsettings.json
??? ASB.Pusher/          # Azure Function pro odesílání zpráv (.NET 8)
    ??? FunctionPushToQueueAsb.cs
    ??? FunctionPushToTopicAsb.cs
    ??? AuthHelper.cs
```

## ?? ASB.Reader - Interaktivní ?te?ka zpráv

Konzolová aplikace pro ?tení a interaktivní zpracování zpráv z Azure Service Bus Queue nebo Topic.

### ? Funkce

- **Výb?r typu entity** p?i startu (Queue nebo Topic + Subscription)
- **Interaktivní zpracování** každé zprávy s možnostmi:
  - ? **Complete** - Ozna?it jako zpracovanou
  - ?? **Abandon** - Vrátit zp?t do fronty
  - ?? **Dead Letter** - P?esunout do Dead Letter Queue
  - ?? **Skip** - Pokra?ovat bez akce
- **Formátované zobrazení** JSON zpráv
- **Podrobné metadata** (ID, ?as, delivery count, properties)
- **?eské uživatelské rozhraní**

### ?? Konfigurace

Upravte `appsettings.json`:

```json
{
  "ServiceBusConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...",
  "ServiceBusQueueName": "your-queue-name",
  "ServiceBusTopicName": "your-topic-name"
}
```

### ?? Spušt?ní

```bash
cd ASB.Reader
dotnet run
```

Aplikace se zeptá na:
1. **Typ entity** - Queue (Q) nebo Topic (T)
2. **Název** - m?žete použít výchozí z konfigurace
3. **Subscription** - jen pro Topic (povinné)

### ?? P?íklad použití

```
?? Azure Service Bus Reader Starting...
============================================================
?? Vyberte typ Service Bus entity:
1??  [Q] Queue - Zpracování zpráv z fronty
2??  [T] Topic - Zpracování zpráv z tématu (vyžaduje subscription)

?? Zadejte volbu (Q/T): T

?? KONFIGURACE TOPIC
?? Zadejte název Topic (default: asb-topic-test): 
?? Zadejte název Subscription (povinné): my-subscription

? Message processor started for Topic: asb-topic-test/my-subscription
?? Waiting for messages... Press Ctrl+C to stop.

================================================================================
?? NOVÁ ZPRÁVA P?IJATA
?? Message ID: 12345-67890
? Enqueued Time: 2024-01-15 14:30:25 UTC
?? Delivery Count: 1
?? Content Type: application/json

?? MESSAGE CONTENT:
----------------------------------------
{
  "Content": "Hello from Azure Functions!",
  "Timestamp": "2024-01-15T14:30:25.123Z",
  "Source": "FunctionPushToAsb"
}
----------------------------------------

?? Co chcete s touto zprávou ud?lat?
1??  [C] Complete - Ozna?it jako zpracovanou (smazat z fronty)
2??  [A] Abandon - Vrátit zp?t do fronty
3??  [D] Dead Letter - P?esunout do Dead Letter Queue
4??  [S] Skip - Pokra?ovat bez akce (zpráva z?stane v lock stavu)

?? Zadejte volbu (C/A/D/S): C
? Zpráva 12345-67890 byla ozna?ena jako zpracovaná (Complete).
```

## ? ASB.Pusher - Azure Functions

Obsahuje dv? Azure Functions pro automatické generování zpráv:

### ?? FunctionPushToQueueAsb
- Odesílá zprávy do **Queue** každých 5 sekund
- Generuje náhodný text s metadaty
- Používá Timer Trigger

### ?? FunctionPushToTopicAsb  
- Odesílá zprávy do **Topic** každých 5 sekund
- Podporuje Managed Identity autentizaci
- Generuje strukturované JSON zprávy

### ?? Konfigurace Functions

Nastavte environment prom?nné:
```
ServiceBusConnectionString=Endpoint=sb://...
ServiceBusQueueName=your-queue
ServiceBusTopicName=your-topic
```

## ??? Technické požadavky

- **ASB.Reader**: .NET 9.0
- **ASB.Pusher**: .NET 8.0 (Azure Functions)
- **NuGet balí?ky**:
  - Azure.Messaging.ServiceBus
  - Microsoft.Extensions.Hosting
  - Microsoft.Extensions.Configuration

## ?? Autentizace

Podporovány jsou dva zp?soby:

### 1. Connection String (Shared Access Key)
```
Endpoint=sb://namespace.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=...
```

### 2. Managed Identity (pouze ASB.Pusher)
```
Endpoint=sb://namespace.servicebus.windows.net/;Authentication=Managed Identity
```

## ?? Troubleshooting

### "ServiceBusConnectionString is required"
- Zkontrolujte `appsettings.json` v ASB.Reader
- Nebo nastavte environment prom?nnou `ServiceBusConnectionString`

### Build chyby v ASB.Pusher
- Chyby s GitHub NuGet jsou neškodné pro ASB.Reader
- ASB.Reader se kompiluje nezávisle

### Connection timeout
- Ov??te správnost connection stringu
- Zkontrolujte network connectivity k Azure

## ?? Poznámky

- **Queue vs Topic**: Queue = point-to-point, Topic = publish-subscribe
- **Subscription**: Pro Topic musíte vytvo?it subscription v Azure Portal
- **Dead Letter**: Zprávy s chybami se automaticky p?esunou po n?kolika neúsp?šných pokusech
- **Lock Duration**: Zprávy mají omezený ?as na zpracování (default 30s)

## ?? P?ípady použití

1. **Debugging zpráv** - Prohlížení obsahu a metadat
2. **Testing message flow** - Ov??ení správného doru?ování
3. **Manual message processing** - Ru?ní ?ešení problematických zpráv
4. **Dead letter investigation** - Analýza neúsp?šných zpráv

---

**Autor:** Azure Service Bus Demo  
**Verze:** 1.0  
**License:** MIT