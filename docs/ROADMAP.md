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

Status: geïmplementeerd in beide hosts; lokale water-/energiebalans-, reset-, replay- en 60/30/15-s-convergentietests geslaagd. GitHub-acceptatie volgt bij publicatie.

## 8. Atmosfeer en chemie

Gasreservoirs, partiële drukken en geselecteerde reacties.

Acceptatie: Elementbalansen sluiten en voorraden worden nooit negatief.

Status: geïmplementeerd in kern, desktop en browser, met scenario’s en CSV. Lokale druk-, balans-, oplossings-, uitgassings-, vacuüm- en replaytests geslaagd. GitHub-acceptatie volgt bij publicatie.

## 9. Eerste leven

Micro-organismen, fotosynthese, ademhaling en nutriënten.

Acceptatie: Groei is begrensd door beschikbare energie en grondstoffen.

Status: Gepland.

## 10. Terraforming

Installaties, energie, productie, transport en ingrepen.

Acceptatie: Iedere ingreep heeft expliciete kosten, doorlooptijd en bijwerkingen.

Status: Gepland.

## 11. Validatie

Referentiegegevens, onzekerheden en gevoeligheidsanalyse.

Acceptatie: Geldigheidsgebied en afwijkingen zijn gedocumenteerd.

Status: Gepland.

## 12. Uitgebreide simulator

Planeettypen, scenario-editor, inspectie en optimalisatie.

Acceptatie: Langdurige experimenten zijn reproduceerbaar en visueel overtuigend.

Status: Gepland.
