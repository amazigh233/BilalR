# Manual Test Plan

Dit testplan stabiliseert MVP 1 zonder nieuwe features toe te voegen. De focus ligt op bedrijven, openingstijden, beschikbaarheid, reserveringen, statuswijzigingen en de publieke booking page.

## Voorbereiding

1. Start SQL Server met de lokale `BookingDb`.
2. Pas migraties toe:
   ```bash
   dotnet ef database update --project src/Booking.Infrastructure/Booking.Infrastructure.csproj --startup-project src/Booking.Api/Booking.Api.csproj
   ```
3. Start de API:
   ```bash
   dotnet run --project src/Booking.Api/Booking.Api.csproj
   ```
4. Start de Blazor app:
   ```bash
   dotnet run --project src/Booking.BlazorApp/Booking.BlazorApp.csproj
   ```
5. Open de Blazor app op de URL uit de console, standaard `http://localhost:5001`.

## Happy Flow

1. Open `/admin/restaurants`.
2. Maak een restaurant aan met naam, optioneel telefoonnummer en optioneel e-mailadres.
3. Controleer dat een succesmelding verschijnt en het restaurant in de lijst staat.
4. Open `Openingstijden` voor het restaurant.
5. Zet minimaal een dag open, bijvoorbeeld maandag van `17:00` tot `22:00`.
6. Sla de openingstijden op en controleer dat een succesmelding verschijnt.
7. Open de publieke pagina via `Publieke pagina`.
8. Vul naam, e-mailadres, aantal personen, datum en tijd binnen openingstijden in.
9. Verstuur de reservering.
10. Controleer dat een succesmelding verschijnt op de publieke pagina.
11. Ga terug naar `/admin/restaurants`.
12. Open `Reserveringen` voor het restaurant.
13. Controleer dat de reservering zichtbaar is met status `New`.
14. Klik `Bevestigen`.
15. Controleer dat een succesmelding verschijnt en de status `Confirmed` is.
16. Test ook `Annuleren` en `No-show` op een testreservering.

## Foutscenario's

### Restaurantformulier

1. Laat `Naam` leeg en verstuur het formulier.
2. Verwacht: validatiemelding `Naam is verplicht.`
3. Vul een ongeldig e-mailadres in.
4. Verwacht: validatiemelding `Gebruik een geldig e-mailadres.`

### Openingstijden

1. Open de openingstijdenpagina van een restaurant.
2. Sla op zonder geopende dag.
3. Verwacht: foutmelding `Kies minimaal een geopende dag.`
4. Zet een dag open van `17:00` tot `01:00`.
5. Verwacht: opslaan lukt; dit betekent open tot na middernacht.
6. Zet een dag open met sluitingstijd gelijk aan openingstijd.
7. Verwacht: foutmelding `Sluitingtijd mag niet gelijk zijn aan openingstijd.`

### Publieke booking page

1. Laat `Naam` leeg en verstuur.
2. Verwacht: validatiemelding `Naam is verplicht.`
3. Laat `E-mail` leeg of vul een ongeldig e-mailadres in.
4. Verwacht: verplichte-veldmelding of e-mailvalidatie.
5. Zet `Aantal personen` op `0`.
6. Verwacht: validatiemelding `Aantal personen moet minimaal 1 zijn.`
7. Kies een tijd buiten openingstijden.
8. Verwacht: foutmelding `Het restaurant is gesloten op het gekozen tijdstip.`
9. Controleer in het reserveringsoverzicht dat er geen reservering is aangemaakt voor dit geweigerde tijdstip.
10. Bij openingstijden `17:00` tot `01:00`: kies `23:30` op de geopende dag.
11. Verwacht: reservering wordt geaccepteerd.
12. Bij dezelfde openingstijden: kies `00:30` op de volgende kalenderdag.
13. Verwacht: reservering wordt geaccepteerd als vervolg van de vorige openingsdag.
14. Bij dezelfde openingstijden: kies `01:00` op de volgende kalenderdag.
15. Verwacht: reservering wordt geweigerd omdat sluitingstijd exclusief is.

### Reserveringsoverzicht

1. Open de reserveringenpagina.
2. Gebruik datumfilter en statusfilter.
3. Verwacht: de teller en tabel tonen alleen passende reserveringen.
4. Klik `Vernieuwen`.
5. Verwacht: loading state tijdens laden en daarna de actuele lijst.
6. Wijzig een status.
7. Verwacht: knop is tijdelijk disabled, daarna succesmelding en bijgewerkte status.

### API-fouten en bereikbaarheid

1. Stop de API en vernieuw een Blazor pagina die API-data nodig heeft.
2. Verwacht: nette foutmelding dat de API tijdelijk niet bereikbaar is.
3. Start de API opnieuw.
4. Roep een niet-bestaand restaurant op, bijvoorbeeld `/booking/00000000-0000-0000-0000-000000000001`.
5. Verwacht: nette foutmelding, geen technische stacktrace.
6. Forceer of simuleer een API 500 tijdens ontwikkeling.
7. Verwacht in Blazor: algemene foutmelding, geen stacktrace voor de gebruiker.

## API-controles

Gebruik Swagger of HTTP-calls om minimaal deze endpoints te controleren:

1. `GET /api/restaurants`
2. `POST /api/restaurants`
3. `GET /api/restaurants/{restaurantId}`
4. `GET /api/restaurants/{restaurantId}/opening-hours`
5. `POST /api/restaurants/{restaurantId}/opening-hours`
6. `GET /api/restaurants/{restaurantId}/availability`
7. `POST /api/reservations`
8. `GET /api/restaurants/{restaurantId}/reservations`
9. `PATCH /api/reservations/{reservationId}/status`

Controleer bij fouten dat de API een duidelijke JSON-fout teruggeeft en geen technische stacktrace.

## Acceptatiecriteria

1. Een klant kan alleen reserveren binnen ingestelde openingstijden.
2. Verplichte velden blokkeren lege of ongeldige invoer.
3. De Blazor UI toont loading, success en error states.
4. API-fouten verschijnen in de UI als nette meldingen.
5. De ondernemer kan reserveringen bekijken en status wijzigen.
6. `dotnet build Booking.sln` slaagt.
7. `dotnet test Booking.sln` slaagt.
