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

## Bevestigde GitHub-validatie milestone 1.5

Workflow: https://github.com/pjotrcasteel/PlanetSimulator/actions/runs/36859077673

Standaard Windows-build, zeven C#-tests, getrimde Blazor-publish op Ubuntu, twee Node-geometriechecks en Chromium-acceptatie zijn geslaagd. Desktop- en mobiele screenshots zijn visueel gecontroleerd. Pages-deployment is geslaagd; live build.json bevestigt commit 39106d74c98a60969e9a14b08e9229aeed58e66e op https://pjotrcasteel.github.io/PlanetSimulator/.

De browserdemo is gepubliceerd. De handmatige acceptatie van de native Windows-app blijft open. Verborgen-tabbladgedrag is geïmplementeerd maar nog niet apart als browseracceptatietest uitgevoerd.

## Milestone 2

Lokale C#-tests: 17 geslaagd. Release-build van alle projecten: geslaagd met nul waarschuwingen en fouten, met dezelfde tijdelijke SDK-cache-workaround als bij milestone 1.5. Desktop-render met bitmapreadouts: uitgevoerd en visueel gecontroleerd. De rekenmodellen hebben geen workaround nodig.

Bevestigd: https://github.com/pjotrcasteel/PlanetSimulator/actions/runs/36862871565 — Windows-build, 17 C#-tests, twee geometriechecks, getrimde Blazor-publish, Chromium en Pages-deployment geslaagd. Native, desktopbrowser- en mobiele screenshots visueel gecontroleerd. Live build.json bevestigde c47a6a3275c0997ead83b5cee1bc26f7417a54db.

## Milestone 3

24 C#-tests controleren ook JSON-replay, alle dagelijkse monsters, CSV-reproduceerbaarheid, geplande wijzigingen zonder temperatuursprong, energiebudgetten, hosttijd-partities, scenario-isolatie, annuleren en versie-/invoervalidatie. Browseracceptatie controleert runnen, vergelijken, daginspectie, JSON-download/import, CSV-herhaling, afwijzing van onbekende modellen en browseropslag na herladen.

De web-job herhaalt de browserexport via de desktop-CLI en vergelijkt elk monster: relatieve tolerantie 1e-10, absolute tolerantie 1e-8 in de kolomeenheid; voor het kleine energiebudgetresidu 0.02 J/m². Exacte CSV-herhaling wordt binnen dezelfde runtime getest; bit-identieke resultaten tussen alle hardware/runtimes worden niet beloofd.

Lokale SDK-cache-workaround blijft beperkt tot de controleomgeving. Definitieve standaardbuild, getrimde publish en browseracceptatie volgen in GitHub Actions. Handmatige native Windows-toetsenbordacceptatie blijft open.

Bevestigde GitHub-validatie: https://github.com/pjotrcasteel/PlanetSimulator/actions/runs/36865991496. Windows-build, 24 tests, twee geometriechecks, standaard getrimde WASM-publish, Chromium-acceptatie, browser/desktop-replayvergelijking en Pages-publicatie geslaagd. Native en browserscreenshots visueel gecontroleerd. Live build.json bevestigde 051ce190d36eb2ac6df9f4d17190bd1909035946. Mobiele SVG-aslabels zijn daarna vergroot voor leesbaarheid.
