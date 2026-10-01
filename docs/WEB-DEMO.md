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

Buildidentiteit staat onderaan de pagina. Geen service worker of offline-cache. Vernieuwen haalt de gepubliceerde app op. Er is geen klimaatmodel in deze milestone.

## Acceptatie

1. De app laadt onder /PlanetSimulator/, zonder ontbrekende WASM- of JS-bestanden.
2. De bol is zichtbaar en kan worden gedraaid en gezoomd.
3. Pauzeren stopt de dagwaarde, maar laat de camera werken.
4. Snelheden veranderen de dagwaarde; reset zet tijd en camera terug.
5. Wireframe werkt en de layout blijft bruikbaar op mobiel.
6. Een verborgen tabblad telt geen extra tijd op.
7. Een nieuwe geslaagde main-build toont de nieuwe commit onderaan.
