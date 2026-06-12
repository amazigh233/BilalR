# Centraal reserveringsplatform

Dit project is een centraal reserveringsplatform voor lokale ondernemers.

Het platform helpt ondernemers die reserveringen en afspraken nu beheren via WhatsApp, telefoon, Instagram, Excel of losse notities. De eerste focus ligt op restaurants en lokale ondernemers, met Sultana BBQ en twee WordPress-klanten als eerste testcases.

## Doel

Het doel van het platform is om reserveringen centraal, overzichtelijk en betrouwbaar te beheren.

De ondernemer moet in MVP 1 kunnen:

- een restaurant of bedrijf aanmaken;
- openingstijden instellen;
- beschikbaarheid controleren;
- reserveringen ontvangen;
- reserveringen bekijken;
- de status van een reservering wijzigen;
- reserveringen beheren via een Blazor dashboard;
- klanten laten reserveren via een publieke booking page.

## Architectuur in het kort

De kern van het systeem is een centrale .NET backend met een centrale SQL database.

Belangrijk uitgangspunt:

WordPress is niet de hoofd-database. Een toekomstige WordPress-plugin is alleen een client/koppeling met de centrale API. Alle reserveringslogica en alle reserveringsdata staan in de centrale .NET backend.

## Tech stack

- .NET 10
- ASP.NET Core Web API
- Blazor Web App
- EF Core
- SQL Server
- Clean Architecture light

## Solution-structuur

De solution gebruikt een lichte Clean Architecture-opzet:

```text
Booking.sln
src/
  Booking.Domain/
  Booking.Application/
  Booking.Infrastructure/
  Booking.Api/
  Booking.BlazorApp/
```

Projectreferenties:

- `Booking.Domain`: geen projectreferenties.
- `Booking.Application`: verwijst naar `Booking.Domain`.
- `Booking.Infrastructure`: verwijst naar `Booking.Application` en `Booking.Domain`.
- `Booking.Api`: verwijst naar `Booking.Application` en `Booking.Infrastructure`.
- `Booking.BlazorApp`: heeft in fase 1 geen projectreferenties en krijgt later communicatie via de API.

## Lokaal bouwen en draaien

Build de volledige solution:

```bash
dotnet build Booking.sln
```

Run de centrale API:

```bash
dotnet run --project src/Booking.Api/Booking.Api.csproj
```

Run de Blazor app:

```bash
dotnet run --project src/Booking.BlazorApp/Booking.BlazorApp.csproj
```

## Restaurantaccounts lokaal aanmaken

Met Docker Compose kan een lokale SuperAdmin worden geseed door in `.env` dev seed aan te zetten:

```text
AUTH_DEV_SEED_ENABLED=true
DEV_SUPERADMIN_EMAIL=superadmin@zambiq.local
DEV_SUPERADMIN_PASSWORD=ChangeThis_LocalSuperAdmin_123!
```

Log daarna in via de Blazor app en open:

```text
http://localhost:5001/admin/restaurant-accounts/create
```

De pagina maakt via `POST /api/restaurant-accounts` een restaurant, een owner user en de `Owner` rolkoppeling aan. Users worden altijd via ASP.NET Identity aangemaakt; wachtwoorden worden niet als plain text opgeslagen.

## Medewerkers lokaal beheren

Restaurant owners kunnen medewerkers voor hun eigen restaurant aanmaken via:

```text
http://localhost:5001/admin/staff
```

De pagina gebruikt `POST /api/admin/staff`, `GET /api/admin/staff`, `PATCH /api/admin/staff/{userId}/disable` en `PATCH /api/admin/staff/{userId}/enable`. Het restaurant wordt altijd server-side bepaald uit de ingelogde owner; `RestaurantId` staat niet in de request body.

## Booking widget

Restaurant owners vinden onder `/admin/widget` een live preview en kant-en-klare installatiecode. De standaard HTML-integratie is:

```html
<div data-zambiq-restaurant="JE-RESTAURANT-ID"></div>
<script src="https://jouw-zambiq-host/widget.js" async></script>
```

`widget.js` plaatst een iframe naar `/embed/booking/{id}` en past de hoogte automatisch aan via `postMessage`. Gebruik voor een vaste hoogte `data-auto-resize="false"` in combinatie met bijvoorbeeld `data-height="760"`.

Voor WordPress staat de plugin in `wordpress-plugin/zambiq-booking`. Na het instellen van de publieke Zambiq-host plaats je `[zambiq_booking restaurant="JE-RESTAURANT-ID"]` op een pagina of in een bericht.

Owners beheren op `/admin/widget` ook de primaire kleur, accentkleur, welkomsttekst en het restaurantlogo. Logo's kunnen als PNG, JPEG of WebP tot 2 MB worden geupload; een externe publieke logo-URL blijft als alternatief beschikbaar. Deze branding wordt per restaurant opgeslagen en direct toegepast op de publieke bookingpagina, iframe-widget en live preview.

Geuploade logo's worden door de API bewaard in `WidgetAssets:StoragePath` en via `WidgetAssets:PublicBaseUrl` publiek geleverd. Docker Compose gebruikt hiervoor het persistente volume `booking-widget-assets` en de waarde van `PUBLIC_API_URL`.

De embed mag standaard alleen binnen dezelfde origin worden geladen. Restaurantowners beheren hun toegestane websites via `/admin/widget`; deze worden per restaurant opgeslagen en direct in de CSP-header toegepast.

Een website moet als origin worden ingevoerd, bijvoorbeeld `https://www.restaurant.nl` of lokaal `http://restaurant.local`. Paden zoals `/reserveren` horen niet in de allowlist.

Voor een platformbrede nood-allowlist kan de beheerder aanvullende origins configureren:

```text
WIDGET_ALLOWED_FRAME_ANCESTORS="'self' https://status.example-platform.nl"
```

Waarden met paden of ongeldige CSP-sources worden geweigerd tijdens het starten. Docker Compose bewaart Data Protection-keys voor de API en Blazor-app in aparte volumes, zodat authenticatie na een containerrestart geldig blijft. De API past openstaande EF Core-migraties toe voordat optionele ontwikkelaccounts worden geseed.

## Boekhouding-light

Restaurantowners beheren onder `/admin/boekhouding` een lichte financiële administratie met:

- handmatige omzet-, kosten- en transferboekingen;
- btw-splits voor 0%, 9% en 21%;
- een controle-inbox voor CSV-, bezorg-, bank- en Molliebronnen;
- private PDF/JPG/PNG-bewijsstukken tot 10 MB;
- CSV- en PDF-periode-exports.

Generieke CSV-import en handmatige invoer blijven beschikbaar zonder externe credentials. Voor directe koppelingen configureer je optioneel `ACCOUNTING_GOCARDLESS_*` en `ACCOUNTING_MOLLIE_*` uit `.env.example`. Originele imports en bewijsstukken staan in het persistente, private Docker-volume `booking-accounting-assets`.

Het btw-overzicht is een conceptoverzicht voor controle door ondernemer of boekhouder en geen officiële aangifte.

## MVP 1

MVP 1 is bewust klein gehouden. De scope staat vast en wordt beschreven in [MVP_SCOPE.md](MVP_SCOPE.md).

## Documentatie

- [MVP_SCOPE.md](MVP_SCOPE.md): exacte scope van MVP 1
- [ARCHITECTURE.md](ARCHITECTURE.md): technische architectuur en grenzen
- [ROADMAP.md](ROADMAP.md): fasering na fase 0
- [THUISBEZORGD_INTEGRATION.md](THUISBEZORGD_INTEGRATION.md): cloud- en lokale T-Connect-integratie

## Status

Fase 0: productrichting, scope en architectuur vastgelegd.

Fase 1: solution en basisstructuur aangemaakt.
