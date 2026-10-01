# Browserdemo en GitHub Pages

## Eenmalige inrichting

In de private repository: Settings → Pages → Build and deployment → Source: GitHub Actions.

GitHub Pages uit een private persoonlijke repository vereist een geschikt abonnement, zoals GitHub Pro. De website is normaal openbaar; de repository blijft private. De browser ontvangt uitvoerbare simulatiecode en kan die downloaden en inspecteren. Een private repository maakt clientcode op een openbare demo niet geheim.

Bronnen:
- https://docs.github.com/en/pages/getting-started-with-github-pages/what-is-github-pages
- https://docs.github.com/en/pages/getting-started-with-github-pages/creating-a-github-pages-site
- https://learn.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly/?view=aspnetcore-10.0

Na activatie: Actions → Build, test and publish → Run workflow op main. Publicatie verschijnt op https://pjotrcasteel.github.io/PlanetSimulator/.

## Automatische updates

De Windows-job bouwt alle projecten en test de C#-kern. Daarna test de web-job de geometrie, publiceert Blazor en configureert het /PlanetSimulator/-subpad. Een webpakket is altijd beschikbaar als workflow-artifact; de deploy-job publiceert naar Pages. Een mislukte build vervangt de bestaande demo niet.

De demo is een browserpresentatie van de gedeelde C#-kern, geen MonoGame-build. JS berekent geen simulatietijd of planetaire rotatie. JS doet alleen WebGL-rendering, camerabewegingen en doorgeven van echte verstreken tijd. Bij een verborgen tabblad stopt tijdopbouw; terugkeren veroorzaakt geen inhaalsprong.

Buildidentiteit staat onderaan de pagina. Geen service worker of offline-cache. Vernieuwen haalt de gepubliceerde app op. Milestone 2 voegt een uniform energiebalansmodel toe. Afstand en albedo zijn instelbaar; de globale temperatuur en energiestromen komen uit C#. Zie SCIENCE.md.

## Acceptatie

1. De app laadt onder /PlanetSimulator/, zonder ontbrekende WASM- of JS-bestanden.
2. De bol is zichtbaar en kan worden gedraaid en gezoomd.
3. Pauzeren stopt de dagwaarde, maar laat de camera werken.
4. Snelheden veranderen de dagwaarde; reset zet tijd en camera terug.
5. Wireframe werkt en de layout blijft bruikbaar op mobiel.
6. Een verborgen tabblad telt geen extra tijd op.
7. Een nieuwe geslaagde main-build toont de nieuwe commit onderaan.

## Milestone 2 acceptatie

- De referentie-evenwichtstemperatuur is circa 254.578 K.
- Wijzig afstand en albedo tijdens pauze: het doel en de flux veranderen, de temperatuur springt niet.
- Kies een week per seconde en hervat: een koudere forcing laat de temperatuur dalen.
- Reset zet temperatuur op 230 K en behoudt de huidige forcing.

## Milestone 3 acceptatie

- Start een run; grafiek en daginspectie tonen N+1 monsters.
- Plan een verandering: nieuwe evenwichtstemperatuur op de geplande dag, zonder directe temperatuursprong.
- Voer een tweede run uit en vergelijk beide curves.
- Download JSON/CSV, laad JSON opnieuw en vergelijk de nieuwe CSV met de oorspronkelijke export.
- Bewaar/laden in browser werkt ook na paginaherladen.
- Onbekende modelversie geeft een fout zonder het huidige formulier te overschrijven.
- Dezelfde browser-JSON wordt via de desktop-CLI herhaald; alle monsters moeten binnen gedocumenteerde toleranties overeenkomen.

## Milestone 4 acceptatie

De demo start regionaal. Uniform model blijft beschikbaar voor eerdere scenario’s. Modelwisselen herstart de tijd. Een dag per seconde is de bovengrens voor live regionale weergave. De temperatuurkaart toont celdata op de vaste schaal 170–330 K, ook aan de nachtzijde. Ashelling en warmtetransport zijn continu wijzigbaar zonder temperatuurreset.

Controleer de 288 cellen, ontwikkelende temperatuurverschillen, sluitend energiebudget, behoud van parameters bij reset, kaart/gewoon oppervlak, mobiele layout, regionale scenario-import/export en vergelijking van iedere eindcel met de desktophost. Het experimentformulier kiest zijn eigen model, onafhankelijk van de live planeet.
