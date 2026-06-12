# Architectuur

Dit document beschrijft de technische architectuur voor het centrale reserveringsplatform.

---

## Google Business Profile Integration

### Source of Truth

| Data type | Source of truth | Sync direction | Trigger |
|---|---|---|---|
| Opening hours (regular) | Zambiq | Zambiq → GBP | On save + manual "Sync now" |
| Business name / phone | Zambiq restaurant entity | Zambiq → GBP | Manual (future) |
| Reviews | Google | GBP → Zambiq | Background sync every 6 hours |
| Review replies | Zambiq (user writes) | Zambiq → GBP | On submit |
| Food menu | Zambiq menu catalog (not built yet) | Zambiq → GBP | Deferred |
| Photos / Posts | Created in Zambiq | Zambiq → GBP | Future |

**Rule:** Google is never the source of truth for structured data Zambiq already owns
(hours, business info). Zambiq is never the source of truth for user-generated content
that originates on Google (reviews).

### Token Storage

OAuth tokens are encrypted with **ASP.NET Core Data Protection API** (protector key:
`Zambiq.GoogleBusiness.v1`) before storage in `GoogleBusinessConnections`. Tokens are
decrypted in-memory only when needed. Expired access tokens are refreshed automatically;
refresh failure marks the connection `ReconnectRequired`.

### Connection Lifecycle

```
(no connection) → Pending → Connected ←→ ReconnectRequired
                                │
                                └→ Disabled
```

### API Surface

| Feature | API | Notes |
|---|---|---|
| List accounts/locations | Account Management API v1 (NuGet) | Post-approval quota ~300 QPM |
| Update hours | Business Information API v1 (NuGet) | `PATCH locations/{name}?updateMask=regularHours` |
| Get/reply/delete reviews | My Business API v4 (raw HTTP) | Still required in 2026; no NuGet |
| OAuth token exchange | Google OAuth2 (raw HTTP) | `oauth2.googleapis.com/token` |

### Background Sync

`GoogleBusinessReviewSyncService` (IHostedService, PeriodicTimer 6h) — same pattern as
`AccountingDailySyncService`. Reviews are upserted by `ReviewName`. Errors logged + stored
on connection; service never crashes on individual failure.

### Feature Flag

Entire integration gated by `GoogleBusiness:Enabled` (default `false`). All endpoints
return 404 when disabled. Set to `true` only after GBP API access approved in GCP.
See `SETUP.md` for the full setup procedure.

## Kernprincipe

De centrale .NET backend is de bron van waarheid.

Alle reserveringslogica en alle reserveringsdata staan in de centrale backend en centrale SQL database.

WordPress is niet de hoofd-database. Een toekomstige WordPress-plugin is alleen een client van de API.

## Hoofdcomponenten

Het platform bestaat uit de volgende hoofdcomponenten:

- centrale ASP.NET Core Web API;
- centrale SQL Server database;
- Blazor dashboard voor beheer;
- publieke booking page;
- later een WordPress-plugin als API-client;
- later een embed widget voor elke website.

## Tech stack

- .NET 10
- ASP.NET Core Web API
- Blazor Web App
- EF Core
- SQL Server
- Clean Architecture light

## Solution-structuur

De beoogde structuur:

```text
src/
  Booking.Domain/
  Booking.Application/
  Booking.Infrastructure/
  Booking.Api/
  Booking.BlazorApp/
```

## Laagverdeling

### Booking.Domain

Bevat de kernmodellen en domeinregels.

Voorbeelden:

- bedrijf of restaurant;
- openingstijden;
- reservering;
- reserveringsstatus.

De domain-laag bevat geen databasecode, geen API-code en geen UI-code.

### Booking.Application

Bevat use cases en applicatielogica.

Voorbeelden:

- bedrijf aanmaken;
- openingstijden instellen;
- beschikbaarheid controleren;
- reservering aanmaken;
- reserveringen ophalen;
- reserveringsstatus wijzigen.

De application-laag bepaalt wat het systeem doet, zonder afhankelijk te zijn van concrete infrastructuur.

### Booking.Infrastructure

Bevat technische implementaties.

Voorbeelden:

- EF Core DbContext;
- SQL Server databaseconfiguratie;
- repository-implementaties als die nodig zijn;
- voorbereiding voor e-mailnotificaties.

### Booking.Api

Bevat de centrale ASP.NET Core Web API.

De API is de toegangspoort voor:

- het Blazor dashboard;
- de publieke booking page;
- een toekomstige WordPress-plugin;
- een toekomstige embed widget.

### Booking.BlazorApp

Bevat de Blazor Web App.

Deze app bevat:

- beheeromgeving voor ondernemers;
- reserveringsoverzicht;
- statusbeheer;
- publieke booking page.

## Datamodel voor MVP 1

MVP 1 heeft minimaal modellen nodig voor:

- bedrijven/restaurants;
- openingstijden;
- reserveringen;
- reserveringsstatussen.

De reserveringsstatussen zijn:

- New
- Confirmed
- Cancelled
- NoShow

## API-regels

De API moet de centrale bron blijven voor reserveringslogica.

Clients mogen geen eigen reserveringslogica bevatten die afwijkt van de backend. Dit geldt voor:

- Blazor dashboard;
- publieke booking page;
- toekomstige WordPress-plugin;
- toekomstige embed widget.

## WordPress-regel

De toekomstige WordPress-plugin:

- slaat geen reserveringen op als hoofdbron;
- bevat geen eigen reserveringslogica;
- leest en schrijft via de centrale API;
- dient alleen als koppeling tussen een WordPress-site en het centrale platform.

Als WordPress tijdelijk lokale configuratie nodig heeft, mag dat alleen voor plugin-instellingen. Reserveringsdata en bedrijfslogica blijven centraal.

## MVP 1-grenzen

De architectuur mag technisch voorbereid zijn op groei, maar MVP 1 bouwt alleen wat nodig is voor:

- bedrijven beheren;
- openingstijden beheren;
- beschikbaarheid controleren;
- reserveringen aanmaken;
- reserveringen bekijken;
- status wijzigen;
- basis e-mailnotificatie voorbereiden;
- Blazor dashboard;
- publieke booking page.

Functionaliteit buiten deze lijst hoort niet in MVP 1.
