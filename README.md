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

- .NET 9
- ASP.NET Core Web API
- Blazor Web App
- EF Core
- SQL Server
- Clean Architecture light

## Projectstructuur

De beoogde solution-structuur:

```text
src/
  Booking.Domain/
  Booking.Application/
  Booking.Infrastructure/
  Booking.Api/
  Booking.BlazorApp/
```

## MVP 1

MVP 1 is bewust klein gehouden. De scope staat vast en wordt beschreven in [MVP_SCOPE.md](MVP_SCOPE.md).

## Documentatie

- [MVP_SCOPE.md](MVP_SCOPE.md): exacte scope van MVP 1
- [ARCHITECTURE.md](ARCHITECTURE.md): technische architectuur en grenzen
- [ROADMAP.md](ROADMAP.md): fasering na fase 0

## Status

Fase 0: productrichting, scope en architectuur vastleggen.
