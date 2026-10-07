# Reproduceerbare experimenten

## Browser

Het experimentpaneel staat onder de interactieve planeet. Kies een planeettype of stel naam, duur (1–3.650 dagen), meetinterval, begincondities en optioneel wijzigingen van afstand en Bond-albedo in. Klik **Experiment uitvoeren**. Een experiment start altijd op dag 0 en gebruikt niet de huidige temperatuur of tijd van de planeet erboven. De vorige voltooide run blijft als paarse vergelijkingscurve staan. Annuleren behoudt de laatste voltooide resultaten.

De groene curve is berekende temperatuur; de gouden curve het onmiddellijke stralingsevenwicht. Een wijziging op dag 30 wordt na precies 30 dagen integreren toegepast. Het dag-30-monster heeft de temperatuur van vóór de wijziging en de nieuwe forcing/evenwichtstemperatuur. Warmtecapaciteit en sterhelderheid blijven gedurende de run constant. Wijzigingen moeten strikt oplopende gehele dagen zijn, tussen start en einde; maximaal 32.

Gebruik de dagschuif om meetpunten te inspecteren. Dagelijkse monsters zijn geen dagelijkse integratiestappen: intern blijft de stap 60 seconden. Grafieklijnen verbinden monsters; reacties korter dan één dag kunnen tussen monsters liggen. De evenwichtscurve toont wijzigingen als verticale sprongen.

**Scenario van run** hoort bij de getoonde CSV. **Scenario (JSON)** en **Bewaar in browser** gebruiken de huidige formulierwaarden, die na een run gewijzigd kunnen zijn. Laden/importeren verandert het formulier en start geen berekening; vorige resultaten blijven zichtbaar. Browseropslag bewaart één scenario op dit apparaat; download JSON voor overdracht en duurzame bewaring. Er wordt geen scenariodata naar een server gestuurd.

## Desktop en CLI

G rekent het geladen scenario door; zonder geladen scenario gebruikt het de huidige afstand/albedo en de ingestelde oorspronkelijke begintemperatuur, 365 dagen. De grafiek verschijnt vanaf 900 pixels vensterbreedte en vergelijkt met de vorige run. S bewaart het scenario van de laatste voltooide run en zijn CSV in `experiments/`. Zonder voltooide run bewaart S alleen het geladen/huidige scenario. L leest `experiments/scenario.json`; G voert het daarna uit. Afstand/albedo handmatig wijzigen kiest weer een nieuw scenario met de huidige live instellingen.

Een browser-JSON is ook zonder grafische sessie door te rekenen:

```powershell
dotnet run --project src/PlanetSimulator.Desktop -c Release -- --experiment examples/cooling.json --output experiments/cooling
```

Uitvoer: `scenario.json` en `results.csv`. Met Ctrl+C wordt de CLI-run afgebroken. Gebruik een aparte uitvoermap per experiment; deze twee bestanden worden bij opnieuw uitvoeren overschreven.

## Bestandsformaat en reproduceerbaarheid

Nieuwe exports gebruiken formatversie 2. De nieuwe eigenschap `sampleEveryDays` bepaalt alleen welke dagen worden geëxporteerd; de numerieke integratie
blijft iedere 60 seconden plaatsvinden. Format-v1-bestanden, waaronder de bestaande voorbeeldscenario's, worden automatisch gemigreerd naar v2 met
`sampleEveryDays = 1`. Onbekende toekomstige formatversies blijven geweigerd.

Zie [cooling.json](../examples/cooling.json). De modelidentiteit `global-blackbody-rk4-60s-v1` leggen de huidige vergelijkingen en numerieke stap vast. Alle vijf klimaatbeginwaarden staan expliciet in SI-eenheden, behalve afstand (AU), albedo en relatieve sterhelderheid. Een scenario is een recept, geen opslag van een lopende simulatie. Huidige modeltoestand, renderer en camerastand worden niet opgeslagen.

De importer weigert onbekende versies/velden, ontbrekende beginwaarden, ongeldige waarden en bestanden boven 64 KiB. Nieuwe vergelijkingen of integratieregels moeten een nieuwe modelidentiteit krijgen; oude scenario’s mogen niet stilzwijgend een ander model gebruiken.

CSV is UTF-8, komma-gescheiden, met invariant decimaalpunt en round-trip getallen. Kolomkoppen bevatten SI-eenheden. Dag 0 plus één monster per voltooide dag geeft N+1 monsters. CSV bevat geen formulecellen of vrije scenarionamen. Bewaar het bijbehorende JSON-recept voor modelidentiteit en beginwaarden.

Tests eisen identieke monsters/CSV na JSON-replay binnen dezelfde runtime. Een CI-controle herhaalt een echte WASM-browserexport in de desktophost en vergelijkt alle kolommen met expliciete toleranties (zie VALIDATION.md). Dit is geen bewijs van natuurkundige onderzoeksnauwkeurigheid; de aannames uit SCIENCE.md blijven gelden.

## Regionale experimenten (milestone 4)

Kies in het experimentformulier **Regionaal · 288 cellen**. Ashelling, D en jaarlengte horen bij het JSON-recept. Begin met 3–30 dagen; regionale runs rekenen iedere cel uit en duren langer. De groene grafiek is het oppervlaktegemiddelde. Daginspectie toont ook celuitersten en hemisfeergemiddelden. Dagelijkse CSV heeft vier extra temperatuurkolommen. **Laatste regiokaart (CSV)** bevat alle 288 eindtemperaturen met coördinaten en celoppervlakte. Die kaart betreft het einde van de run, ongeacht de gekozen inspectiedag.

Modelidentiteit: `regional-blackbody-rk4-60s-12x24-v1`. Oude uniforme scenario’s blijven `global-blackbody-rk4-60s-v1`; geen stilzwijgende omzetting. Dezelfde daggrenzen en forcingwijzigingen gelden. Zie [regional-seasons.json](../examples/regional-seasons.json). De desktop-CLI schrijft bij regionale scenario’s ook `regions.csv`. CI vergelijkt zowel dagelijkse monsters als iedere eindcel tussen browser en desktop.

Desktop C wisselt model en herstart de tijd; T kiest kaart/gewoon oppervlak; O/K wijzigen ashelling; H/J wijzigen D. Parameterwijzigingen bewaren huidige temperaturen. G zonder geladen scenario kiest 30 dagen regionaal of 365 dagen uniform. S bewaart scenario en laatste resultaten; L plus G herhaalt een geladen recept.


## Waterexperimenten

Kies `Land, water en ijs`. De JSON bevat `surface.reliefMeters` en `surface.waterEquivalentDepthMeters` en modelversie `surface-enthalpy-rk4-60s-12x24-v1`. CSV voegt totale watermassa, vloeibare massafractie en massa-afwijking toe. De regionale eindkaart bevat ook hoogte, totale watermassa, vloeibare massa en ijsmassa per m². Oude globale en regionale exports houden hun modelidentiteit en kolommen.

`examples/shallow-melting.json` begint op het smeltpunt met een vlakke wereld en 1 cm water. Bij deze temperatuur is de beginfase ijs; sterke instraling smelt het overdag. Herhaal met luminositeit 0 en begintemperatuur 273,16 K om bevriezen te onderzoeken. Dit is een faseproef bij voorgeschreven druk, geen echte klimaatvoorspelling.

## Waterkringloop en atmosfeer (milestones 7–8)

`examples/hydrology.json` gebruikt modelidentiteit `hydrology-rk4-60s-12x24-v1`. De JSON voegt tijdschalen voor damp, wolken, neerslag en afvoer toe. De dagelijkse CSV bevat totale damp- en wolkenmassa, cumulatieve fluxen en de waterbudgetafwijking; de regionale CSV voegt die velden per cel toe.

`examples/atmosphere-chemistry.json` gebruikt `atmosphere-chemistry-rk4-60s-12x24-v1`. De atmosfeer specificeert totale druk en molfracties voor N₂, O₂, CO₂ en Ar plus de eindige korstvoorraad. De CSV exporteert droge oppervlaktedruk, CO₂-partiële druk, opgeloste CO₂ en C/O/N-budgetfouten. De atmosfeer kan zelfstandig op een regionaal model worden gezet; voeg `surface` toe voor CO₂-opname in vloeibaar water en optioneel `hydrology` voor de waterkringloop. De optische gloed is niet aan deze gasvoorraad gekoppeld.


## Eerste leven (milestone 9)

Kies **Eerste leven · micro-organismen**, begin op 285 K en probeer eerst 2–30 dagen. Beginbiomassa en totale fosforvoorraad zijn instelbaar. De daginspectie toont biomassa en beschikbaar fosfor; CSV bevat productie, ademhaling, chemische energie, gebonden-water-equivalent en de P-balans. De regiokaart bevat biomassa en beschikbaar P per cel. Begin met nul inoculum om te controleren dat leven niet spontaan ontstaat, of kies sterhelderheid nul om ademhaling zonder fotosynthese te onderzoeken.

Het scenario [first-life.json](../examples/first-life.json) koppelt hydrologie, chemie en biologie. Modelidentiteit: `microbial-ch2o-rk4-60s-12x24-v1`; parameters staan onder `atmosphere.biology`. Oudere modellen houden hun eigen identiteit. Een scenario met biologie maar zonder oppervlaktewatermodel wordt afgewezen.

Desktop: **I** wisselt de levenslaag en herstart; **P** kiest de warme start. **G** rekent het experiment, **S** bewaart de resultaten. Via de CLI:

```sh
dotnet run --project src/PlanetSimulator.Desktop -- --experiment examples/first-life.json --output experiments/first-life
```


## Terraforming (milestone 10)

Kies **Terraforming · installaties** in het experimentformulier. Begin met 2 dagen. Het standaardplan bevat een verwarmer en CO₂-afvang in regio 144 plus transport naar regio 145. De transportbouw begint op dag 0,5; reistijd telt daarna vanaf iedere daadwerkelijke verzending.

De editor biedt toevoegen/verwijderen van installaties, type, bron/doel (0–287), start- en stopdag, bouwtijd, vermogen, debiet en reistijd. Beginenergie, bouwvoorraad en tankcapaciteit zijn instelbaar. Technische energie-/materiaalcoëfficiënten staan zichtbaar bij iedere installatie en zijn in JSON aanpasbaar; import behoudt ze. Geen installaties is een bruikbare controlerun. Vermogen 0 schakelt een installatie volledig uit, zonder bouwkosten. Wisselen van live model of **F** in MonoGame herstart de simulatie.

Gebruik [terraforming.json](../examples/terraforming.json) voor het volledige voorbeeld met hydrologie, chemie en biologie. Modelversie: `terraforming-inventory-rk4-60s-12x24-v1`; plannen staan onder `atmosphere.terraforming.installations`. De regionale CSV bevat resterende energie/materialen en CO₂ in tanks/onderweg. De dagelijkse CSV bevat ook verbruik, bouwkosten, productie, leveringen en drie technische budgetresiduen. Iedere run is via JSON herhaalbaar in de desktop-CLI:

```sh
dotnet run --project src/PlanetSimulator.Desktop -- --experiment examples/terraforming.json --output experiments/terraforming
```

Voor causale vergelijking: draai eerst een scenario met installaties en daarna dezelfde instellingen met een leeg installatieplan. Vergelijk vooral de bron-/doelcellen in de regionale CSV: één gewijzigde cel heeft maar een klein effect op de planeetgemiddelde temperatuur. De live uitlezing toont daarom ook de lokale temperatuur per installatie. Minder CO₂ is nog geen berekende broeikaskoeling.


## Uitgebreide simulator (milestone 12)

De selector **Planeettype** vult de bestaande editor met één van zeven synthetische startpunten. Daarna zijn alle parameters gewoon bewerkbaar; een
preset is dus geen verborgen moeilijkheidsgraad en geen nieuwe natuurkundige motor. De catalogus bevat een kale rotswereld, ijswereld, oceaanwereld,
dunne atmosfeer, microbiële wereld, terraforming-kandidaat en een tienjarige globale klimaatreferentie.

Langetermijnruns mogen maximaal 3.650 dagen duren. Gebruik voor jarenlange experimenten een grotere meetinterval, bijvoorbeeld 30 dagen. Het systeem
bewaart altijd dag 0, iedere dag waarop een geplande forcingwijziging wordt toegepast en de laatste simulatiedag. Daardoor blijft het exacte causale
moment van een wijziging zichtbaar, ook wanneer tussendagen niet in CSV staan.

Na een run toont de browser een generieke eindinspectie. Die vat berekende toestand samen maar kent geen score als “bewoonbaar” toe. Waar het gekozen
model data heeft, kunnen ook vloeibaar-waterfractie, luchtdruk, CO₂, biomassa en engineeringverbruik worden geïnspecteerd.

Voor een eenvoudige reproduceerbare parameterzoektocht ondersteunt de desktop-CLI het uniforme klimaatmodel:

```powershell
dotnet run --project src/PlanetSimulator.Desktop -c Release -- --optimize-albedo scenario.json 288 0.1 0.8 15 --output experiments/albedo-search
```

Argumenten zijn achtereenvolgens scenario, doeltemperatuur in kelvin, minimale albedo, maximale albedo en aantal gelijkmatig verdeelde kandidaten
(2–101). Geplande forcingwijzigingen en regionale modellen worden bewust geweigerd. De uitvoermap bevat `optimization.csv`, het reproduceerbare
`best-scenario.json` en `best-results.csv`. Dit is een deterministische grid search van één parameter, geen algemene optimizer en geen bewijs dat
een gevonden reflectiviteit fysiek of technisch bereikbaar is.
