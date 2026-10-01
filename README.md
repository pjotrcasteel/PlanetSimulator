# PlanetSimulator

Een C# / MonoGame-project dat iteratief groeit naar een wetenschappelijk onderbouwde terraforming-simulator met een overtuigende 3D-planeet.

**Live demo:** https://pjotrcasteel.github.io/PlanetSimulator/

## Milestone 2: licht en temperatuur

Bekijk een eenvoudige belichte 3D-planeet en verander afstand tot de ster en reflectie. De gedeelde C#-kern berekent een uniforme temperatuur, warmtestraling en energiebalans. De werkelijke temperatuur verandert geleidelijk; de berekende evenwichtstemperatuur reageert direct op nieuwe instellingen.

Dit is een model zonder atmosfeer of broeikaseffect. Kleuren en rooster zijn decoratief; er zijn nog geen continenten, oceanen, regionale temperaturen of ecosystemen. De referentieplaneet heeft aardachtige massa en straal, met 24 uur rotatietijd. De modeltemperatuur is geen voorspelling voor de huidige aarde.

## Starten

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
| Afsluiten | Escape |

De browser heeft knoppen en schuifregelaars voor dezelfde functies. Reset herstart de temperatuur op 230 K en behoudt je gekozen afstand, reflectie, pauzestatus en snelheid. Beide hosts starten met één simulatiedag per echte seconde. Een verborgen browsertabblad bouwt geen tijd op.

## Architectuur en controles

- Simulation: double-precisie, SI-eenheden, vaste stappen van 60 seconden, RK4 en energiebudgetten.
- Desktop: MonoGame-presentatie, orbitcamera en ingebouwde bitmaptekst.
- Web: Blazor WebAssembly met dezelfde C#-kern; WebGL 2 doet uitsluitend weergave en camerabediening.
- Tests: 17 C#-tests, twee Node-geometriechecks en browseracceptatie op de gepubliceerde bestanden.

Node-checks: `node --test tests/browser/geometry.test.mjs`. De Chromium-controle gebruikt `npm ci`, `npx playwright install chromium` en `npm run test:smoke -- artifacts/web/wwwroot` op een gepubliceerd pakket met het /PlanetSimulator/-basepad.

GitHub Actions bouwt en test op Windows, maakt een zelfstandige Windows-download en publiceert na geslaagde browsercontroles naar Pages. Downloadbare pakketten en screenshots staan bij de workflow. De repository blijft private.

Zie [wetenschappelijke aannames](docs/SCIENCE.md), [roadmap](docs/ROADMAP.md), [validatiestatus](docs/VALIDATION.md) en [Pages-inrichting](docs/WEB-DEMO.md).
