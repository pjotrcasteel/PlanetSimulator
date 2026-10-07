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

## Milestone 4

35 lokale C#-tests, inclusief 11 regionale controles. Roosteroppervlak, globale instraling, interne warmtestromen, seizoenen, tijdstapverkleining, energiebudget, framepartities en iedere cel na JSON-replay zijn gecontroleerd. De absolute somfout op het boloppervlak is afronding bij circa 5.1×10^14 m²; daarom gebruikt de oppervlaktecontrole een relatieve tolerantie 1e-12.

Browseracceptatie: modelkeuze, kaart aan/uit, celtemperatuurverschillen, ashelling/D zonder temperatuursprong, budget, reset, regionale JSON-replay, dagelijkse CSV en 288-cellenexport. CI herhaalt het regionale browserrecept in de desktophost en vergelijkt zowel dagelijks CSV als eindcellen. De SDK-cache-workaround blijft lokaal; standaard builds en getrimde publish worden in GitHub gecontroleerd. Handmatige interactieve native Windows-bediening blijft open.

Bevestigd op GitHub: https://github.com/pjotrcasteel/PlanetSimulator/actions/runs/36871866976. Alle jobs geslaagd: Windows-build met 35 tests, standaard getrimde Blazor-publicatie, geometrie, Chromium, uniforme én regionale hostreplay en Pages. Alle 288 eindcellen en dagelijkse regionale kolommen vergeleken. De browsertest vond eerst een bool-selectbinding; die is vervangen door expliciete modelkeuze en daarna volledig opnieuw gecontroleerd. Native en browserscreenshots zijn visueel gecontroleerd. Live build.json bevestigde 80d440f6a3c704e363a06d772eaa7347c114c20b. Een lokaal regionaal CLI-experiment van 30 dagen kostte 0.91 seconde; dit is één meting in de werkruimte en geen browser- of hardwareonafhankelijke performancebelofte.


## Milestone 5 — water en enthalpie

Gecontroleerd op commit `ce155806954d9a0c98f30c38addfb60e60bfd6f7`, [workflow 36887172223](https://github.com/pjotrcasteel/PlanetSimulator/actions/runs/36887172223). Alle jobs inclusief Pages-deployment geslaagd.

- 42 C#-tests: de bestaande 35 plus zeven tests voor enthalpie/latente warmte, basin-vulling, smelten en bevriezen, nul-waterlimiet, tijdstapgevoeligheid, scenario-replay en validatie/cancellation.
- Directe faseproef: temperatuur blijft 273,15 K van nul tot volledige latente energie; daarna volgt voelbare warmte. Warmte- en massabalansen worden ook in de gekoppelde stralings-/diffusiestappen gecontroleerd.
- 60 versus 30 s bij een faseovergang: ieder temperatuurverschil binnen 0,001 K en vloeibare massafractie binnen 0,0001 voor de testproef. Dit is geen algemene foutgrens voor alle scenario's.
- Gekoppelde proef: energieafwijking <0,02 J/m²; relatieve watermassa-afwijking <1e-12, niet-negatieve fasemassa's.
- Windows Release-build en self-contained desktoppublicatie; getrimde Blazor-publicatie; twee JS-geometriechecks; Chromium-bediening, JSON-replay en responsive layout.
- Browserexport herhaald door dezelfde C#-kern via de desktophost: dagelijkse waterdiagnostiek én temperatuur/hoogte/water/liquid/ice van alle 288 eindcellen vergeleken met relatieve tolerantie 1e-10, absolute 1e-8 (energieresidu 0,02 J/m²).
- Native oppervlaktescreenshot, browseroppervlakte en mobiele preview visueel gecontroleerd. Dit valideert implementatie en numerieke afspraken; wetenschappelijke validatie tegen echte oceanen of planetaire waarnemingen is nog niet uitgevoerd.

## Milestones 7–8 — hydrologie, atmosfeer en chemie

Lokale verificatie: 60 C#-tests, twee browsergeometriechecks, Release-build van alle projecten en getrimde Blazor-publicatie geslaagd. Water/energie, 60/30/15-s-convergentie, druk uit molfracties, C/O/N-balansen, vacuüm, eindige uitgassing, oplossen/vrijgave, reset, annuleren en JSON/CSV-replay worden getest. Massaresiduen gebruiken 1e-12 van de voorraad; energie 0,02 J/m². Dit is numerieke validatie, geen kalibratie tegen een echte planeet.

De tijdelijke Linux-omgeving vereist een lokale SDK-cache-aanpassing om WebAssembly/ILLink-taken in hetzelfde proces te draaien (TaskHostFactory kan hier geen socket openen). Deze aanpassing zit niet in de repository. De ongewijzigde SDK, Chromium en hostvergelijking worden in GitHub Actions gecontroleerd.

Bevestigd op 2 oktober 2026: [PR-workflow 36973895257](https://github.com/pjotrcasteel/PlanetSimulator/actions/runs/36973895257) en [main-workflow 36974855939](https://github.com/pjotrcasteel/PlanetSimulator/actions/runs/36974855939) zijn geslaagd. De standaard Windows-build, C#-tests, getrimde Blazor-publicatie, Chromium-acceptatie en browser/desktop-replay van hydrologie en chemie zijn hiermee gecontroleerd. De main-workflow publiceerde commit `51f694e21905b997a914195401d5dba065ad54bf` naar Pages. Handmatige native Windows-bediening en wetenschappelijke kalibratie blijven open.


## Milestone 9 — micro-organismen

69 lokale C#-tests geslaagd, inclusief negen nieuwe tests: gekoppelde stof- en energiebalansen met/zonder hydrologie; donkerrespiratie; zuurstofgebrek; ontbreken van CO₂, P of inoculum; koude/hete/droge habitats; lichtenergie- en nutriëntenbegrenzing; JSON/CSV-replay; 60/30/15-s-convergentie; validatie, annuleren en reset. Massa gebruikt relatieve tolerantie 1e-12 van de betrokken voorraad, energie 0,02 J/m². Het verschil in biomassa tussen 60 en 15 s blijft binnen 0,2% in de vastgelegde eendaagse proef; dit is geen algemene foutgrens.

Browseracceptatie en desktop-replay zijn uitgebreid met een biologisch scenario, dagelijkse biologische kolommen en alle 288 eindcellen. Bevestigd: [workflow 37044410629](https://github.com/pjotrcasteel/PlanetSimulator/actions/runs/37044410629) op commit `15dad372e59a9a004c07a99d77db979108dfb1d9` is geslaagd: Windows-build en 69 tests, getrimde Blazor-publicatie, Chromium en volledige browser/desktop-replay inclusief biologie. Groei-/ademhalingssnelheden en grenzen zijn illustratief en nog niet tegen echte microbiële datasets gevalideerd.

Een CLI-run van 30 dagen met `examples/first-life.json` leverde 31 meetpunten. Maximale absolute energieafwijking: 1,71×10⁻⁵ J/m²; waterafwijking circa 2,02×10⁴ kg op circa 1,53×10¹⁸ kg. Dit zijn afrondingsresiduen, geen verdwenen fysieke voorraden. Native toetsenbordbediening blijft handmatig te controleren.


## Milestone 10 — installatiekosten en logistiek

Lokale Release-build van alle projecten: geslaagd, geen waarschuwingen of fouten. 81 C#-tests geslaagd, waarvan twaalf voor terraforming: eindige energie en verwarming, precieze bouw/start/stopgrenzen, gedeelde bouwvoorraden, onvoldoende bouwenergie, tankcapaciteit en elementbehoud, vertraagde levering na stillegging, afstandskosten, ontbrekende aanvoer/uitgeschakelde installaties, alle gekoppelde budgetten, scenario-replay/reset, validatie/annuleren en 60/30/15-s-convergentie.

Een gekoppelde CLI-proef van 30 dagen met `examples/terraforming.json` leverde 31 meetpunten. Maximale absolute afwijking: 1,82×10⁻⁵ J/m² voor het planetaire energiebudget, 8,95×10⁻⁸ J/m² voor de technische energievoorraad, nul voor bouwmateriaal en 0,0012 kg logistiek op circa 1,90×10¹² kg afgevangen CO₂. Eindige energie raakt op; de simulatie gaat daarna zonder verdere technische toevoer door.

De browsertest controleert de nieuwe modelkeuze en editor, veranderde energievoorraad, JSON/CSV-replay, productie en levering. De GitHub-workflow vergelijkt alle dagelijkse kolommen en alle 288 eindcellen met de desktophost. Bevestigd: [workflow 37099311885](https://github.com/pjotrcasteel/PlanetSimulator/actions/runs/37099311885) op commit `5ca93ff641dca7566759df9fa59b16ea11400fd8` is volledig geslaagd: Windows met 81 tests, getrimde Blazor-publicatie, Chromium, JSON/CSV-replay en vergelijking van alle dagelijkse kolommen en 288 eindcellen met de desktop, inclusief terraforming. Handmatige native toetsenbordacceptatie en kalibratie van technische coëfficiënten blijven open.


## Milestone 11 — wetenschappelijke referenties en gevoeligheid

Milestone 11 voegt een afzonderlijke validatielaag toe bovenop de bestaande behouds-, convergentie-, replay- en hostvergelijkingstests. Het doel is niet
om de volledige gekoppelde simulator al als gekalibreerd planeetmodel te bestempelen, maar om expliciet vast te leggen welke delen tegen externe
referenties worden vergeleken, welke toleranties gelden en waar bekende fysica ontbreekt.

De eerste benchmarkset gebruikt vijf gevallen:

- zonne-instraling op 1 AU: 1361 W/m²;
- globaal geabsorbeerde zonneflux voor een aardachtige Bond-albedo: referentie circa 240 W/m²;
- effectieve stralingstemperatuur van de aarde: referentie circa 255 K;
- gemiddelde aardse oppervlaktetemperatuur: circa 288 K, bewust gemarkeerd als `expected-gap` omdat het huidige klimaatmodel geen langgolvige
  atmosferische broeikasfysica bevat;
- Stefan-Boltzmann-emissie bij 288 K: circa 390 W/m².

Bronnen: NASA Earth Observatory / NASA GISS voor het aardse energiebudget, effectieve temperatuur, oppervlaktetemperatuur en emissie. De toleranties
zijn benchmarkafspraken voor deze specifieke controles en geen algemene foutgrenzen voor andere planeten of scenario's.

De gevoeligheidsanalyse is deterministisch en one-at-a-time. Afstand, Bond-albedo, relatieve sterhelderheid en areale warmtecapaciteit worden elk rond
de standaardwaarde gevarieerd. Iedere variant start vanuit dezelfde 230 K en wordt 30 simulatiedagen geïntegreerd. Het rapport bevat de lage,
standaard- en hoge eindtemperatuur en een dimensieloze genormaliseerde gevoeligheid. Hierdoor zijn richting en relatieve sterkte van de respons
machineleesbaar zonder te suggereren dat parameteronzekerheden al statistisch gekalibreerd zijn.

CLI:

```powershell
dotnet run --project src/PlanetSimulator.Desktop -c Release -- --validate --output artifacts/validation
```

Uitvoer:

- `validation.json`: volledig, versieerbaar rapport;
- `validation.csv`: referentiegevallen, afwijkingen, toleranties, status en bron;
- `sensitivity.csv`: vaste parameterperturbaties en genormaliseerde temperatuurrespons.

Een echte onzekerheidsverdeling, Monte Carlo-ensemble, observationele kalibratie van hydrologie/chemie/biologie/terraforming en een gevalideerd
broeikaseffect blijven buiten deze milestone. Die beperkingen blijven onderdeel van het geldigheidsgebied.


Bevestigd op GitHub in [workflow 37647349679](https://github.com/pjotrcasteel/PlanetSimulator/actions/runs/37647349679): Windows Release-build met
nul compilerwaarschuwingen en nul fouten, 85/85 C#-tests, validatie-CLI met 4 passes, 1 expected gap en 0 failures, getrimde Blazor-publicatie,
Chromium-acceptatie en volledige browser/desktop-replay zijn geslaagd. Het CI-artifact `validation-report` bevat de JSON- en CSV-uitvoer van de
referentie- en gevoeligheidsanalyse.
