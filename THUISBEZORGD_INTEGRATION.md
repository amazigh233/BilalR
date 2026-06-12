# Thuisbezorgd-integratie

Zambiq ondersteunt twee technische routes voor inkomende Thuisbezorgd-orders. Beide
routes gebruiken hetzelfde restaurantgebonden secret en plaatsen orders idempotent in
het bestaande bezorgorderoverzicht.

## 1. Cloudroute: JET Connect ingress

Endpoint:

```text
POST /api/delivery/thuisbezorgd/jet-connect/orders
X-JET-Connect-Secret: <restaurant-secret>
```

Voor productie moet `Delivery:PublicBaseUrl` naar een publiek HTTPS-adres wijzen.
De huidige ingress gebruikt het genormaliseerde Zambiq-ordercontract:

```json
{
  "externalOrderId": "JET-1001",
  "customerName": "Jan Jansen",
  "customerPhone": "0612345678",
  "deliveryAddress": "Straat 1, Amsterdam",
  "note": null,
  "status": "Confirmed",
  "placedAtUtc": "2026-06-08T18:30:00Z",
  "totalAmount": 24.50,
  "currency": "EUR",
  "items": [
    { "name": "Pizza Margherita", "quantity": 1, "unitPrice": 12.00 }
  ]
}
```

Just Eat Takeaway moet Zambiq eerst als JET Connect/POS-partner onboarden. Zodra zij
de definitieve order-specificatie en authenticatiemethode leveren, wordt hun payload
naar dit genormaliseerde contract gemapt en doorloopt de koppeling hun certificering.

## 2. Lokale route: T-Connect XML Connector

De officiële T-Connect XML Client van Thuisbezorgd schrijft orders naar:

```text
C:\Program Files\Takeaway\Tconnect\temp\in
```

De Zambiq companion-app leest de XML, uploadt de order en verplaatst het bestand naar
`ok`. Ongeldige of door de API afgewezen bestanden gaan naar `nok`.

Publiceer een zelfstandige Windows-build:

```powershell
dotnet publish src/Booking.TConnectConnector/Booking.TConnectConnector.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -o artifacts/tconnect-connector
```

Start op de restaurant-pc:

```powershell
Booking.TConnectConnector.exe `
  --api-url https://api.example.nl `
  --secret <restaurant-secret>
```

De mappen zijn aanpasbaar met `--in`, `--ok` en `--nok`. T-Connect zelf en de
restaurantkoppeling moeten door Thuisbezorgd worden geleverd en geactiveerd.

## Configuratie en veiligheid

- Stel in Docker/VPS `PUBLIC_API_URL=https://api.example.nl` in.
- Secrets worden alleen eenmalig getoond; uitsluitend de SHA-256-hash wordt opgeslagen.
- Vernieuwen van een secret maakt het vorige secret direct ongeldig.
- Beide ingressroutes zijn rate-limited en accepteren alleen een actief secret.
- De bestaande generieke testwebhook blijft beschikbaar voor ontwikkeling.

Deze fase ondersteunt inkomende orders. Menu-sync, productkoppelcodes, orderacceptatie
en statusupdates terug naar Thuisbezorgd vereisen de officiële JET-specificaties en
vallen buiten de huidige ingress.
