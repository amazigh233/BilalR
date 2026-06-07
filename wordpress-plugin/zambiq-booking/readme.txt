=== Zambiq Booking ===
Contributors: zambiq
Tags: reserveringen, booking, widget, restaurant
Requires at least: 6.3
Tested up to: 6.5
Stable tag: 1.0.0
License: GPLv2 or later

Sluit de Zambiq-reserveringswidget in op je WordPress-site via een shortcode.
WordPress is enkel een client van de centrale Zambiq-API; reserveringen en data
blijven in de centrale Zambiq-backend.

== Installatie ==

1. Zip de map `zambiq-booking` (zodat je `zambiq-booking.zip` krijgt met daarin `zambiq-booking.php`).
2. Ga in WordPress naar Plugins → Nieuwe plugin → Plugin uploaden, en upload de zip.
3. Activeer de plugin.
4. Ga naar Instellingen → Zambiq Booking en vul de **Widget host URL** in
   (de publieke URL van je Zambiq-omgeving, bijv. https://app.zambiq.nl). Dit is geen geheim.

== Gebruik ==

Plaats op een pagina of bericht de shortcode:

    [zambiq_booking restaurant="JE-RESTAURANT-ID"]

Optioneel een vaste hoogte (in pixels):

    [zambiq_booking restaurant="JE-RESTAURANT-ID" height="760"]

Het restaurant-id is de GUID van het restaurant in Zambiq (te vinden in het Zambiq-dashboard
of in de publieke booking-URL `/booking/{id}`).

== Hoe het werkt ==

De shortcode plaatst een container `<div data-zambiq-restaurant="...">` en laadt
`widget.js` vanaf de ingestelde host. Dat script plaatst een responsive iframe naar
`/embed/booking/{id}` op je Zambiq-omgeving. Er worden geen reserveringsgegevens in
WordPress opgeslagen.

== Changelog ==

= 1.0.0 =
* Eerste versie: shortcode + instellingen voor de widget host URL.
