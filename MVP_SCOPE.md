# MVP Scope

Alles wat niet expliciet in MVP 1 staat, wordt niet gebouwd.

Dit document beschrijft de vaste scope van MVP 1 voor het centrale reserveringsplatform.

## Product

Een centraal reserveringsplatform voor lokale ondernemers.

## Doelgroep

De eerste doelgroep bestaat uit:

- restaurants;
- lokale ondernemers;
- Sultana BBQ als eerste testcase;
- twee bestaande WordPress-klanten als eerste testcases.

## Probleem

Veel ondernemers beheren reserveringen en afspraken nu via losse kanalen:

- WhatsApp;
- telefoon;
- Instagram;
- Excel;
- losse notities.

Dit zorgt voor versnippering, kans op fouten en weinig overzicht.

## MVP 1 bevat

MVP 1 bevat alleen de volgende onderdelen.

### Bedrijf beheren

- restaurant of bedrijf aanmaken;
- basisgegevens van het bedrijf opslaan;
- technisch voorbereiden op meerdere bedrijven in het centrale platform.

Multi-vestiging wordt niet functioneel uitgewerkt in MVP 1, behalve als technische voorbereiding logisch nodig is.

### Openingstijden

- openingstijden instellen per bedrijf;
- openingstijden gebruiken bij beschikbaarheidscontrole.

### Beschikbaarheid

- beschikbaarheid controleren op basis van ingestelde openingstijden;
- voorkomen dat reserveringen buiten openingstijden worden aangemaakt.

### Reserveringen

- reservering aanmaken;
- reserveringen bekijken;
- reserveringsstatus wijzigen.

Ondersteunde statussen:

- New
- Confirmed
- Cancelled
- NoShow

### Notificaties

- basis e-mailnotificatie voorbereiden.

In MVP 1 betekent dit dat de code en structuur voorbereid worden op e-mailnotificaties. Uitgebreide e-mailflows, templates en marketingmails horen niet bij MVP 1.

### Beheeromgeving

- Blazor dashboard voor ondernemers/beheer;
- overzicht van reserveringen;
- mogelijkheid om status te wijzigen.

### Publieke booking page

- publieke pagina waarop klanten een reservering kunnen maken;
- de publieke pagina gebruikt de centrale API en centrale database.

## MVP 1 bevat expliciet niet

De volgende onderdelen worden niet gebouwd in MVP 1:

- native mobiele app;
- boekhoudsysteem;
- betalingen;
- kassakoppeling;
- tafelplattegrond;
- QR-bestellen;
- roostersysteem;
- urenregistratie;
- AI-functionaliteit;
- uitgebreide analytics;
- multi-vestiging, behalve eventueel technisch voorbereid.

## Scopebewaking

Nieuwe ideeen of wensen worden niet automatisch toegevoegd aan MVP 1.

Als een wens niet direct nodig is voor de onderdelen hierboven, gaat deze naar de roadmap of backlog voor later.

Het doel van MVP 1 is om snel te valideren of lokale ondernemers waarde halen uit een centraal reserveringssysteem.

## Beslisregel

Als een nieuwe feature niet nodig is om een reservering aan te maken, te bekijken of van status te wijzigen, gaat deze naar "Later" en niet naar MVP 1.