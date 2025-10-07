# Azure Service Bus Reader & Pusher

Uk�zkov� aplikace pro pr�ci s Azure Service Bus - ?ten� a odes�l�n� zpr�v do Queue a Topic.

## ?? Struktura projektu

```
example-ASB/
??? ASB.Reader/          # Konzolov� aplikace pro ?ten� zpr�v (.NET 9)
?   ??? Program.cs
?   ??? Helpers/
?   ?   ??? ServiceBusMessageReader.cs
?   ??? appsettings.json
??? ASB.Pusher/          # Azure Function pro odes�l�n� zpr�v (.NET 8)
    ??? FunctionPushToQueueAsb.cs
    ??? FunctionPushToTopicAsb.cs
    ??? AuthHelper.cs
```

## ?? ASB.Reader - Interaktivn� ?te?ka zpr�v

Konzolov� aplikace pro ?ten� a interaktivn� zpracov�n� zpr�v z Azure Service Bus Queue nebo Topic.

### ? Funkce

- **V�b?r typu entity** p?i startu (Queue nebo Topic + Subscription)
- **Interaktivn� zpracov�n�** ka�d� zpr�vy s mo�nostmi:
  - ? **Complete** - Ozna?it jako zpracovanou
  - ?? **Abandon** - Vr�tit zp?t do fronty
  - ?? **Dead Letter** - P?esunout do Dead Letter Queue
  - ?? **Skip** - Pokra?ovat bez akce
- **Form�tovan� zobrazen�** JSON zpr�v
- **Podrobn� metadata** (ID, ?as, delivery count, properties)
- **?esk� u�ivatelsk� rozhran�**

### ?? Konfigurace

Upravte `appsettings.json`:

```json
{
  "ServiceBusConnectionString": "Endpoint=sb://your-namespace.servicebus.windows.net/;SharedAccessKeyName=...",
  "ServiceBusQueueName": "your-queue-name",
  "ServiceBusTopicName": "your-topic-name"
}
```

### ?? Spu�t?n�

```bash
cd ASB.Reader
dotnet run
```

Aplikace se zept� na:
1. **Typ entity** - Queue (Q) nebo Topic (T)
2. **N�zev** - m?�ete pou��t v�choz� z konfigurace
3. **Subscription** - jen pro Topic (povinn�)

### ?? P?�klad pou�it�

```
?? Azure Service Bus Reader Starting...
============================================================
?? Vyberte typ Service Bus entity:
1??  [Q] Queue - Zpracov�n� zpr�v z fronty
2??  [T] Topic - Zpracov�n� zpr�v z t�matu (vy�aduje subscription)

?? Zadejte volbu (Q/T): T

?? KONFIGURACE TOPIC
?? Zadejte n�zev Topic (default: asb-topic-test): 
?? Zadejte n�zev Subscription (povinn�): my-subscription

? Message processor started for Topic: asb-topic-test/my-subscription
?? Waiting for messages... Press Ctrl+C to stop.

================================================================================
?? NOV� ZPR�VA P?IJATA
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

?? Co chcete s touto zpr�vou ud?lat?
1??  [C] Complete - Ozna?it jako zpracovanou (smazat z fronty)
2??  [A] Abandon - Vr�tit zp?t do fronty
3??  [D] Dead Letter - P?esunout do Dead Letter Queue
4??  [S] Skip - Pokra?ovat bez akce (zpr�va z?stane v lock stavu)

?? Zadejte volbu (C/A/D/S): C
? Zpr�va 12345-67890 byla ozna?ena jako zpracovan� (Complete).
```

## ? ASB.Pusher - Azure Functions

Obsahuje dv? Azure Functions pro automatick� generov�n� zpr�v:

### ?? FunctionPushToQueueAsb
- Odes�l� zpr�vy do **Queue** ka�d�ch 5 sekund
- Generuje n�hodn� text s metadaty
- Pou��v� Timer Trigger

### ?? FunctionPushToTopicAsb  
- Odes�l� zpr�vy do **Topic** ka�d�ch 5 sekund
- Podporuje Managed Identity autentizaci
- Generuje strukturovan� JSON zpr�vy

### ?? Konfigurace Functions

Nastavte environment prom?nn�:
```
ServiceBusConnectionString=Endpoint=sb://...
ServiceBusQueueName=your-queue
ServiceBusTopicName=your-topic
```

## ??? Technick� po�adavky

- **ASB.Reader**: .NET 9.0
- **ASB.Pusher**: .NET 8.0 (Azure Functions)
- **NuGet bal�?ky**:
  - Azure.Messaging.ServiceBus
  - Microsoft.Extensions.Hosting
  - Microsoft.Extensions.Configuration

## ?? Autentizace

Podporov�ny jsou dva zp?soby:

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
- Chyby s GitHub NuGet jsou ne�kodn� pro ASB.Reader
- ASB.Reader se kompiluje nez�visle

### Connection timeout
- Ov??te spr�vnost connection stringu
- Zkontrolujte network connectivity k Azure

## ?? Pozn�mky

- **Queue vs Topic**: Queue = point-to-point, Topic = publish-subscribe
- **Subscription**: Pro Topic mus�te vytvo?it subscription v Azure Portal
- **Dead Letter**: Zpr�vy s chybami se automaticky p?esunou po n?kolika ne�sp?�n�ch pokusech
- **Lock Duration**: Zpr�vy maj� omezen� ?as na zpracov�n� (default 30s)

## ?? P?�pady pou�it�

1. **Debugging zpr�v** - Prohl�en� obsahu a metadat
2. **Testing message flow** - Ov??en� spr�vn�ho doru?ov�n�
3. **Manual message processing** - Ru?n� ?e�en� problematick�ch zpr�v
4. **Dead letter investigation** - Anal�za ne�sp?�n�ch zpr�v

---

**Autor:** Azure Service Bus Demo  
**Verze:** 1.0  
**License:** MIT