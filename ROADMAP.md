# Roadmap

Deze roadmap beschrijft de fasering van het centrale reserveringsplatform.

De productrichting staat vast. Dit document is bedoeld om uitvoering en scope te bewaken, niet om opnieuw te brainstormen.

## Fase 0: Fundament en documentatie

Doel: de richting, scope en architectuur vastleggen voordat de bouw start.

Output:

- README.md;
- MVP_SCOPE.md;
- ARCHITECTURE.md;
- ROADMAP.md.

Beslissingen:

- het platform wordt centraal gebouwd;
- de backend wordt een .NET 9 Web API;
- data wordt opgeslagen in een centrale SQL Server database;
- het dashboard wordt gebouwd met Blazor;
- WordPress wordt later alleen een client van de API;
- MVP 1 blijft beperkt tot de afgesproken reserveringsfunctionaliteit.

## Fase 1: Solution en basisstructuur

Doel: de technische basis klaarzetten.

Werk:

- .NET solution aanmaken;
- projecten aanmaken voor Domain, Application, Infrastructure, Api en BlazorApp;
- basisreferenties tussen projecten instellen;
- centrale configuratie voorbereiden;
- lokale ontwikkelomgeving werkend maken.

## Fase 2: Domein en database

Doel: de kernmodellen en opslag voor MVP 1 bouwen.

Werk:

- model voor bedrijf/restaurant;
- model voor openingstijden;
- model voor reservering;
- enum of vaste waarden voor reserveringsstatus;
- EF Core DbContext;
- SQL Server configuratie;
- eerste database migrations.

## Fase 3: Application use cases

Doel: de applicatielogica voor MVP 1 bouwen.

Werk:

- bedrijf aanmaken;
- openingstijden instellen;
- beschikbaarheid controleren;
- reservering aanmaken;
- reserveringen ophalen;
- reserveringsstatus wijzigen;
- basisstructuur voor e-mailnotificatie voorbereiden.

## Fase 4: Centrale API

Doel: MVP-functionaliteit beschikbaar maken via de centrale Web API.

Werk:

- endpoints voor bedrijven;
- endpoints voor openingstijden;
- endpoint voor beschikbaarheidscontrole;
- endpoints voor reserveringen;
- endpoint voor statuswijziging.

De API blijft de enige plek waar reserveringslogica wordt afgedwongen.

## Fase 5: Blazor dashboard

Doel: ondernemers reserveringen laten beheren.

Werk:

- dashboardbasis;
- reserveringsoverzicht;
- status wijzigen;
- bedrijf en openingstijden beheren.

## Fase 6: Publieke booking page

Doel: klanten online een reservering laten maken.

Werk:

- publieke reserveringspagina;
- beschikbaarheid tonen/controleren;
- reservering aanmaken via de centrale API.

## Fase 7: Eerste testcases

Doel: MVP 1 valideren met echte ondernemers.

Testcases:

- Sultana BBQ;
- twee bestaande WordPress-klanten.

Werk:

- testdata inrichten;
- reserveringsflow testen;
- dashboardflow testen;
- feedback verzamelen op bruikbaarheid en betrouwbaarheid.

## Later

Na MVP 1 kunnen koppelingen en distributiekanalen worden toegevoegd.

Bekende latere onderdelen:

- WordPress-plugin als client van de centrale API;
- embed widget voor elke website.

Ook later blijft gelden:

WordPress is niet de hoofd-database. Alle reserveringslogica en reserveringsdata blijven in de centrale .NET backend.
