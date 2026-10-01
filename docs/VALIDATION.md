# Validatiestatus

Gecontroleerd op 1 oktober 2026 met .NET SDK 10.0.401:

- Volledige Release-build: geslaagd, nul waarschuwingen en nul fouten.
- MSTest: alle zes tests geslaagd.
- DesktopGL-start met SDL offscreen: geslaagd.
- Render naar PNG: uitgevoerd en visueel gecontroleerd; volledige belichte bol zichtbaar.
- Alle C#-bestanden: UTF-8 met BOM en regels maximaal 180 tekens.

Interactieve Windows-bediening is hier nog niet handmatig getest. De Windows-CI is voorbereid maar nog niet op GitHub uitgevoerd. Repository: https://github.com/pjotrcasteel/PlanetSimulator (private).

Handmatige acceptatie op Windows:

1. Start de app: een belichte bol met decoratief rooster moet zichtbaar zijn.
2. Sleep met links en zoom met het muiswiel; ga niet door het oppervlak heen.
3. Pauzeer met spatie; dagwaarde blijft gelijk, camera blijft werken.
4. Hervat; kies 1, 2 en 3 en controleer de snelheid in de venstertitel.
5. Druk W voor wireframe; druk opnieuw voor vaste vlakken.
6. Vergroot en verklein het venster; de bol blijft rond.
7. Druk R; dagwaarde en camera keren terug naar de uitgangspositie.
8. Escape sluit de app.

Geen wetenschappelijke klimaatsimulatie in deze milestone; uitsluitend tijd, rotatie en basisplaneeteigenschappen.

## Milestone 1.5

- C#-tests: zeven geslaagd, inclusief overeenkomst van desktop- en browsersnapshots.
- Node-geometriechecks: twee geslaagd (unit sphere, indices, seam en orbitcamera).
- Blazor-build en statische publish lokaal gecontroleerd. Deze werkomgeving blokkeert MSBuild-taskhosts; voor de lokale controle is alleen de tijdelijke SDK-cache aangepast en trimming uitgeschakeld. De repository bevat die aanpassingen niet. De standaard Release-build en getrimde publish worden definitief in GitHub Actions gecontroleerd.
- Pages-basepad /PlanetSimulator/ gecontroleerd in het gepubliceerde index.html.
- Chromium-acceptatie en screenshots zijn onderdeel van de web-job. De lokale browserdownload was niet beschikbaar; de echte browsercontrole wordt op GitHub uitgevoerd.
- Eenmalige Pages-activatie en live bereikbaarheid moeten worden bevestigd voordat de demo als gepubliceerd geldt.
