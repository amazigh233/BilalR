# Zambiq T-Connect Connector

Deze Windows companion-app leest Thuisbezorgd T-Connect XML-bestanden uit de lokale
`in`-map en uploadt ze naar de Zambiq API. Na succesvolle verwerking verhuist het
bestand naar `ok`; blijvend ongeldige XML of een afgewezen request verhuist naar `nok`.

## Starten

Maak in Zambiq onder `Bezorgkoppelingen` een technisch secret aan en start:

```powershell
dotnet run --project src/Booking.TConnectConnector -- `
  --api-url https://api.example.nl `
  --secret YOUR_SECRET
```

Standaardmappen:

```text
C:\Program Files\Takeaway\Tconnect\temp\in
C:\Program Files\Takeaway\Tconnect\temp\ok
C:\Program Files\Takeaway\Tconnect\temp\nok
```

Deze zijn overschrijfbaar via `--in`, `--ok`, `--nok` of de omgevingsvariabelen
`ZAMBIQ_TCONNECT_IN`, `ZAMBIQ_TCONNECT_OK` en `ZAMBIQ_TCONNECT_NOK`.

Gebruik `--once` om één scan uit te voeren. Productiegebruik vereist dat
Thuisbezorgd de T-Connect XML Client voor het restaurant activeert.
