# Roadmap

We ontwikkelen wetenschap en presentatie iteratief. Milestone 1 begint eenvoudig; een prachtige planeet blijft het visuele einddoel. Validatie begint meteen en wordt bij milestone 11 systematisch uitgebreid. Geen belofte van onderzoeksnauwkeurigheid zonder meetbare validatie.

## 1. Eerste planeet

3D-bol, orbitcamera, klok, pauze, reset en tests.

Acceptatie: De app start en de camera werkt ook bij pauze.

Status: Geïmplementeerd; build, zes tests en offscreen-render geslaagd. Interactieve Windows-acceptatie staat nog open.

## 1.5. Browserdemo

Blazor WebAssembly met dezelfde C#-simulatiekern en een WebGL 2-renderer. Draaien, zoomen, pauze, reset, snelheidskeuze en wireframe. Alleen de presentatie verschilt van de desktopapp. De browserdemo pauzeert automatisch zolang het tabblad verborgen is.

Acceptatie: desktop- en browsersnapshots komen overeen; build en publish slagen; bediening en Pages-subpad werken. Iedere geslaagde wijziging op main publiceert de laatste demo. Buildidentiteit staat onderaan de pagina.

Status: voltooid. Windows-build, zeven C#-tests, twee geometriechecks, Chromium-acceptatie en getrimde publicatie geslaagd. Live: https://pjotrcasteel.github.io/PlanetSimulator/.

## 2. Licht en temperatuur

Globale stralingsbalans met inverse kwadratenwet, Bond-albedo, warmtecapaciteit en Stefan–Boltzmann-emissie. Desktop en browser tonen temperatuur, evenwicht en energiestromen. Afstand en reflectie zijn als experimenten instelbaar. Eén week per seconde maakt de reactie zichtbaar.

Acceptatie: analytisch referentie-evenwicht, energiebudget, stapgevoeligheid, warming/cooling en overeenkomst tussen hosts worden getest. Zie SCIENCE.md voor aannames en beperkingen.

Status: voltooid. Windows-build, 17 C#-tests, twee geometriechecks, getrimde Blazor-publish, Chromium en Pages-publicatie geslaagd.

## 3. Experimenten

Beginwaarden, geplande forcingwijzigingen, dagelijkse temperatuurgrafiek, vergelijking met de vorige run, inspectie en versieerbare JSON-scenario’s. CSV bevat meetdata en energiebudgetten. Browseropslag en dezelfde experimentrunner in de desktopapp.

Acceptatie: opgeslagen en opnieuw geladen scenario’s leveren dezelfde volledige meetreeks. De browserexport wordt met de desktophost herhaald en numeriek vergeleken. Onbekende modelversies worden geweigerd.

Status: voltooid. 24 C#-tests, Windows-build, Chromium-scenarioacceptatie, JSON/CSV-replay, vergelijking van WASM- en desktopmonsters en Pages-deployment geslaagd. Native en browserscreenshots visueel gecontroleerd.

## 4. Regionaal klimaat

Sferisch rooster van 12 × 24 gelijke-area-cellen, dag-/nachtinstraling, ashelling, een voorgeschreven seizoenscyclus en conservatieve diffusie tussen buurlocaties. Temperatuurkaart op de bol, lokale uitersten en hemisfeergemiddelden. Regionale scenario’s met eigen modelidentiteit en CSV-regiokaart.

Acceptatie: roosteroppervlak is 4πR²; geïntegreerde zoninstraling klopt; intern transport behoudt warmte; energiebudget sluit; tijdstapconvergentie, JSON-replay en hostvergelijking worden getest.

Status: voltooid. Windows-build, 35 C#-tests, twee geometriechecks, getrimde Blazor-publish, Chromium-acceptatie, herhaling van dagelijkse regionale data en alle 288 eindcellen via desktop en Pages-publicatie geslaagd. Native, desktopbrowser- en mobiele screenshots visueel gecontroleerd.

## 5. Land, water en ijs

Hoogtekaart, waterreservoirs en faseovergangen.

Acceptatie: Watermassa blijft behouden; latente warmte wordt meegenomen.

Status: voltooid. Synthetische hoogtekaart, basin-vulling, enthalpie, fasefracties, desktop-/browserkaart en versieerbare waterexperimenten. Windows-build, 42 C#-tests, twee geometriechecks, getrimde publicatie, Chromium-acceptatie, dagelijkse waterdiagnostiek en alle 288 eindreservoirs via WASM/desktop, en Pages-deployment geslaagd. Native en browserscreenshots gecontroleerd. Geen stroming, atmosfeer of ijsalbedo; zie SCIENCE.md.

## 6. Planeetlook

Gedeelde terreingeometrie, fijnere kustlijnen, rots-/water-/ijsmaterialen, zonreflectie en een schakelbare optische atmosfeer. Koude en warme startwerelden; instelbaar visueel reliëf.

Acceptatie: Visuele toestand volgt de modeldata en blijft interactief.

Status: geïmplementeerd; 47 C#-tests en lokale desktop-/webbuilds geslaagd. Publicatieacceptatie loopt bij deze commit. Optische sfeer is nog niet gekoppeld aan druk of klimaat; reliëf en microdetail zijn expliciete visualisaties.

## 7. Weer en waterkringloop

Vocht, condensatie, neerslag, afvoer en benaderde circulatie.

Acceptatie: Waterbudget sluit en convergentie bij kleinere tijdstappen is onderzocht.

Status: voltooid. Water-/energiebalans, reset, replay, 60/30/15-s-convergentie en GitHub-browser/desktop-acceptatie geslaagd; gepubliceerd.

## 8. Atmosfeer en chemie

Gasreservoirs, partiële drukken en geselecteerde reacties.

Acceptatie: Elementbalansen sluiten en voorraden worden nooit negatief.

Status: voltooid. Druk-, balans-, oplossings-, uitgassings-, vacuüm- en replaytests en GitHub-browser/desktop-acceptatie geslaagd; gepubliceerd.

## 9. Eerste leven

Micro-organismen, fotosynthese, ademhaling en nutriënten.

Acceptatie: Groei is begrensd door beschikbare energie en grondstoffen.

Status: voltooid. Eén geïnoculeerde microbenpool met CH₂O-equivalent, lichtbegrensde fotosynthese, zuurstofbegrensde ademhaling en eindige fosforquota. Water en chemische energie tellen mee in de balansen. Beide hosts, JSON/CSV, 69 tests inclusief negen biologische tests. Windows-build, getrimde Blazor-publicatie, Chromium en browser/desktop-vergelijking van biologische dagmonsters en alle 288 eindcellen geslaagd in workflow 37044410629. Geen abiogenese, ecosysteem of gekalibreerde bewoonbaarheidsvoorspelling.

Open fysica vóór bewoonbaarheidsclaims: broeikaseffect, drukafhankelijke waterfasen en uitgebreidere oceaanchemie. Deze zijn geen onderdeel van het eenvoudige levensmodel.

## 10. Terraforming

Installaties, energie, productie, transport en ingrepen.

Acceptatie: Iedere ingreep heeft expliciete kosten, doorlooptijd en bijwerkingen.

Status: voltooid. Regionale verwarmers, CO₂-afvang en CO₂-transport, eindige bouw- en energievoorraden, bouwtijd, start/stop, volle tanks en vertraagde leveringen. Energie wordt als warmte geboekt; gas in tanks en onderweg blijft in C/O-balansen. Browsereditor, MonoGame-bediening, scenario’s en CSV; 81 lokale tests en een 30-daagse gekoppelde proef geslaagd. GitHub-acceptatie geslaagd in workflow 37099311885: Windows, Chromium en volledige browser/desktop-replay. Geen automatisch bewijs van bewoonbaarheid; ontbrekende broeikasfysica blijft expliciet open.

## 11. Validatie

Referentiegegevens, onzekerheden en gevoeligheidsanalyse.

Acceptatie: Geldigheidsgebied en afwijkingen zijn gedocumenteerd.

Status: voltooid. De gedeelde kern bevat reproduceerbare referentiegevallen met expliciete toleranties en bronverwijzingen. Bekende modeltekorten
krijgen de status `expected-gap` in plaats van een kunstmatige pass. Een deterministische one-at-a-time gevoeligheidsanalyse varieert afstand,
Bond-albedo, sterhelderheid en warmtecapaciteit over een vaste 30-daagse horizon. De desktop-CLI exporteert JSON en twee CSV-bestanden; GitHub Actions
publiceert deze als validation-report artifact. PR-validatie is volledig geslaagd in workflow 37647349679: 85 C#-tests, 4 referentiepasses,
1 expected gap, 0 validatiefouten, Chromium en volledige browser/desktop-replay.

## 12. Uitgebreide simulator

Planeettypen, scenario-editor, inspectie en optimalisatie.

Acceptatie: Langdurige experimenten zijn reproduceerbaar en visueel overtuigend.

Status: geïmplementeerd op de milestone-12-branch; definitieve CI-validatie volgt nog. Zeven synthetische planeettypen vullen de bestaande editor met
volledige reproduceerbare scenario's. De maximale horizon is 3.650 dagen met configureerbare sparse sampling; forcinggrenzen en de einddag blijven
altijd meetpunten. Scenarioformaat v2 bewaart de meetinterval en importeert v1 automatisch als dagelijkse sampling. Voltooide runs krijgen generieke
inspectiemetrics. Het uniforme klimaatmodel heeft een deterministische Bond-albedozoeker die alle kandidaten, het beste scenario en de beste meetreeks
exporteert. Geen preset of optimizeruitkomst is een bewoonbaarheidsclaim of gekalibreerde planeetreconstructie.
