# PlanetSimulator

Een C# / MonoGame-project dat iteratief groeit naar een wetenschappelijk onderbouwde terraforming-simulator met een overtuigende 3D-planeet.

**Live demo:** https://pjotrcasteel.github.io/PlanetSimulator/

## Milestone 12: planeetprofielen

De uitgebreide simulator begint met echte fysieke planeetparameters in plaats van één impliciete aardbol. De browserdemo heeft drie reproduceerbare
startprofielen: de bestaande referentiewereld, een kleine droge wereld en een fictieve oceaan-superaarde. Straal, massa en rotatieperiode zijn nu
onderdeel van de gedeelde C#-toestand. Daardoor veranderen celoppervlaktes en totale reservoirs met de planeetgrootte, dag/nacht volgt de gekozen
rotatieperiode en zwaartekracht/readouts volgen massa en straal.

Scenario-JSON kan een `planet`-object met `radiusMeters`, `massKilograms` en `rotationPeriodSeconds` bevatten. Oude format-v1-scenario's zonder
dit object blijven laden als de eerdere referentieplaneet. Zie `examples/small-dry-world.json` voor een compact voorbeeld. De profielen zijn
experiment-startpunten, geen gekalibreerde voorspellingen van aarde, Mars of een exoplaneet.

## Milestone 11: validatie en gevoeligheid

De simulator krijgt nu een reproduceerbare wetenschappelijke validatielaag naast de bestaande numerieke regressietests. De CLI kan referentiegevallen en
deterministische gevoeligheidsanalyses uitvoeren en schrijft `validation.json`, `validation.csv` en `sensitivity.csv`. Bekende modeltekorten worden niet
verstopt als geslaagde tests: het ontbreken van atmosferische broeikasfysica verschijnt bijvoorbeeld expliciet als `expected-gap`.

Voer de validatie lokaal uit met:

```powershell
dotnet run --project src/PlanetSimulator.Desktop -c Release -- --validate --output artifacts/validation
```

GitHub Actions genereert dezelfde artefacten bij iedere build. De eerste referentieset controleert de zonneflux op 1 AU, een aardachtig geabsorbeerd
energiebudget, effectieve stralingstemperatuur en thermische emissie. Daarnaast worden afstand, Bond-albedo, sterhelderheid en warmtecapaciteit over een
vaste 30-daagse horizon gevarieerd om richting en relatieve gevoeligheid van de respons zichtbaar te maken.

## Milestone 10: terraforming-installaties

Oppervlakteverwarmers, CO₂-afvang en transport tussen regio’s hebben nu eindige energie- en bouwvoorraden, bouwtijden, opslagcapaciteit en reistijden. Het microbieel model blijft aangesloten op water, CO₂, zuurstof en de energiebalans. Waterkringloop, droge gasdruk en CO₂-uitwisseling blijven beschikbaar. Kies een koude of warme startwereld en bekijk hoe de berekende toestand verandert.

De planeet heeft 288 cellen met gelijke oppervlaktes, eigen temperaturen, dag/nacht, seizoenen door ashelling en conservatief warmtetransport. De temperatuurkaart volgt de berekende celwaarden op een vaste schaal van 170–330 K. De globale temperatuur en warmtestraling zijn oppervlaktegemiddelden. Kies het uniforme model om oudere experimenten te herhalen.

Maak een scenario, voer een experiment uit en vergelijk de temperatuurcurve met de vorige run. Bewaar JSON-scenario’s en exporteer dagelijkse meetdata als CSV. Beginwaarden en geplande wijzigingen worden door dezelfde C#-kern in browser en desktop doorgerekend.

Bekijk ook een eenvoudige belichte 3D-planeet en verander afstand tot de ster en reflectie. De gedeelde C#-kern berekent temperatuur, warmtestraling en energiebalans voor het gekozen model. De werkelijke temperatuur verandert geleidelijk; de berekende evenwichtstemperatuur reageert direct op nieuwe instellingen.

De regionale kern bevat nu ook een expliciete gasinventaris en een kleine chemieset: N₂, O₂, CO₂ en Ar worden als kolommassa’s bijgehouden, met totale druk uit zwaartekracht en partiële drukken uit molfracties. CO₂ kan naar vloeibaar water oplossen en vanuit een eindige korstvoorraad vrijkomen; C/O/N-balansen en negatieve voorraden worden gecontroleerd. Dit is nog geen volledig broeikaseffect, oceaanchemie of aardmodel. De optische gloed blijft een presentatie-effect. De modeltemperatuur is geen voorspelling voor de huidige aarde.

## Starten

Kies **Terraforming · installaties** in de demo. In het experimentformulier bewerk je het installatieplan en de voorraden; begin met 2–30 dagen. Bekijk per installatie de status en lokale temperatuur. **F** schakelt het standaard installatieplan in de desktopapp (herstart). Kies **Eerste leven · micro-organismen** voor het eerdere biologische model en **Waterwereld · 285 K** voor een warme start. In het experimentformulier kun je beginbiomassa en de totale fosforvoorraad instellen. **I** schakelt micro-organismen in de desktopapp (herstart). Kies **Atmosfeer en CO₂** voor het eerdere model. Kies **Waterwereld · 285 K** om opname in vloeibaar water te volgen. In het experimentformulier kun je droge druk, CO₂ in ppm en uitgassing instellen; de CSV bevat alle dagmonsters en balansen. **Y** schakelt de waterkringloop in de desktopapp; **U** schakelt de gaslaag (beide herstarten de tijd).

Installeer .NET 10 en voer vanuit deze map uit:

```powershell
dotnet restore PlanetSimulator.slnx
dotnet build PlanetSimulator.slnx
dotnet test PlanetSimulator.slnx
dotnet run --project src/PlanetSimulator.Desktop
```

Voor de browserdemo:

```powershell
dotnet run --project src/PlanetSimulator.Web --urls http://localhost:5080
```

Open http://localhost:5080. DesktopGL vereist een grafische desktop en werkende OpenGL-driver. Een IDE moet .NET 10 en slnx ondersteunen. Geen betaalde engine, fonts of content-build-tools nodig.

## Desktopbediening

| Actie | Bediening |
|---|---|
| Camera draaien / zoomen | Linkermuisknop slepen / muiswiel |
| Pauzeren / hervatten | Spatie |
| Realtime / uur / dag / week per seconde | 1 / 2 / 3 / 4 |
| Reflectie verhogen / verlagen | A / Z |
| Afstand verhogen / verlagen | Page Up / Page Down |
| Wireframe | W |
| Temperatuur, tijd en camera resetten | R |
| Regionaal/uniform model kiezen (herstart) | C |
| Temperatuurkaart aan/uit | T |
| Ashelling verhogen/verlagen | O / K |
| Warmtetransport verhogen/verlagen | H / J |
| Experiment starten / geladen scenario herhalen | G |
| Scenario en laatste meetdata opslaan in experiments/ | S |
| experiments/scenario.json laden (daarna G) | L |
| Afsluiten | Escape |

De desktop begint regionaal; G gebruikt dan standaard 30 dagen. Het uniforme model gebruikt 365 dagen. Regionale weergave begrenst tijdsnelheid tot één dag per seconde.

De browser heeft knoppen en schuifregelaars voor dezelfde functies. Reset herstart de temperatuur op 230 K en behoudt je gekozen afstand, reflectie, pauzestatus en snelheid. Beide hosts starten met één simulatiedag per echte seconde. Een verborgen browsertabblad bouwt geen tijd op.

## Architectuur en controles

- Simulation: double-precisie, SI-eenheden, vaste stappen van 60 seconden, RK4 en energiebudgetten.
- Desktop: MonoGame-presentatie, orbitcamera en ingebouwde bitmaptekst.
- Web: Blazor WebAssembly met dezelfde C#-kern; WebGL 2 doet uitsluitend weergave en camerabediening.
- Tests: 90 C#-tests, twee Node-geometriechecks en browseracceptatie op de gepubliceerde bestanden.

Node-checks: `node --test tests/browser/geometry.test.mjs`. De Chromium-controle gebruikt `npm ci`, `npx playwright install chromium` en `npm run test:smoke -- artifacts/web/wwwroot` op een gepubliceerd pakket met het /PlanetSimulator/-basepad.

GitHub Actions bouwt en test op Windows, maakt een zelfstandige Windows-download en publiceert na geslaagde browsercontroles naar Pages. Downloadbare pakketten en screenshots staan bij de workflow.

Zie [scenario’s en experimenten](docs/EXPERIMENTS.md), [wetenschappelijke aannames](docs/SCIENCE.md), [roadmap](docs/ROADMAP.md), [validatiestatus](docs/VALIDATION.md) en [Pages-inrichting](docs/WEB-DEMO.md).


Milestone 5 voegt **Land, water en ijs** toe aan de modelkeuze. Zet de temperatuurkaart uit voor rotsachtig land, blauw water en wit ijs. De fysieke toestand komt uit C#; milestone 6 reconstrueert de onderliggende hoogtefunctie als fijnere geometrie met uitvergroot reliëf. In de desktopapp schakelt **B** waterreservoirs aan/uit (herstart); **T** wisselt de kaart. Experimenten kunnen reliëf en waterinventaris instellen en dagelijkse waterdiagnostiek plus eindkaarten exporteren. Begin rond 273,15 K met ondiep water om de faseovergang snel te onderzoeken. Zie SCIENCE.md voor de vaste-drukaanname en beperkingen.


## Milestone 6 — planeetweergave

De demo start in de nieuwe weergave met een bevroren wereld. Kies **Waterwereld · 285 K** om de oceanen te zien, of **IJswereld · 230 K** voor de koude start. Beide herstarten de tijd en zijn echte beginwaarden: de planeet kan vervolgens verder afkoelen of opwarmen.

- Gedeelde C#-terreingeometrie voor MonoGame en WebGL, met kustlijnen uit dezelfde hoogtefunctie als het model.
- Zichtbare richels en rotsmaterialen, water met zonreflectie en ijs volgens de berekende massafractie.
- Optische atmosfeer met blauwe verstrooiing en planetaire schaduw; schakelbaar zonder klimaateffect.
- Instelbaar uitvergroot reliëf en behoud van de wetenschappelijke temperatuurkaart.
- Desktop: **P** koude/warme start, **N** atmosferische gloed, **V** reliëf aan/uit; **B** watermodel en **T** temperatuurkaart.

`PlanetSimulator.Rendering` bevat de engine-onafhankelijke geometrie en optische referentiefuncties. Kleuren, kleine richels en optische instellingen zijn presentatie. Ze voegen geen water, leven, druk of broeikaseffect toe aan de simulatie. Zie [SCIENCE.md](docs/SCIENCE.md).
