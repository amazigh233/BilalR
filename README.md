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

## MVP 1

MVP 1 is bewust klein gehouden. De scope staat vast en wordt beschreven in [MVP_SCOPE.md](MVP_SCOPE.md).

## Documentatie

- [MVP_SCOPE.md](MVP_SCOPE.md): exacte scope van MVP 1
- [ARCHITECTURE.md](ARCHITECTURE.md): technische architectuur en grenzen
- [ROADMAP.md](ROADMAP.md): fasering na fase 0

## Status

Fase 0: productrichting, scope en architectuur vastgelegd.

Fase 1: solution en basisstructuur aangemaakt.
