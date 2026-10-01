# Reproduceerbare experimenten

## Browser

Het experimentpaneel staat onder de interactieve planeet. Kies naam, duur (1–730 dagen), begincondities en optioneel wijzigingen van afstand en Bond-albedo. Klik **Experiment uitvoeren**. Een experiment start altijd op dag 0 en gebruikt niet de huidige temperatuur of tijd van de planeet erboven. De vorige voltooide run blijft als paarse vergelijkingscurve staan. Annuleren behoudt de laatste voltooide resultaten.

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

Zie [cooling.json](../examples/cooling.json). Formatversie 1 en modelidentiteit `global-blackbody-rk4-60s-v1` leggen de huidige vergelijkingen en numerieke stap vast. Alle vijf klimaatbeginwaarden staan expliciet in SI-eenheden, behalve afstand (AU), albedo en relatieve sterhelderheid. Een scenario is een recept, geen opslag van een lopende simulatie. Huidige modeltoestand, renderer en camerastand worden niet opgeslagen.

De importer weigert onbekende versies/velden, ontbrekende beginwaarden, ongeldige waarden en bestanden boven 64 KiB. Nieuwe vergelijkingen of integratieregels moeten een nieuwe modelidentiteit krijgen; oude scenario’s mogen niet stilzwijgend een ander model gebruiken.

CSV is UTF-8, komma-gescheiden, met invariant decimaalpunt en round-trip getallen. Kolomkoppen bevatten SI-eenheden. Dag 0 plus één monster per voltooide dag geeft N+1 monsters. CSV bevat geen formulecellen of vrije scenarionamen. Bewaar het bijbehorende JSON-recept voor modelidentiteit en beginwaarden.

Tests eisen identieke monsters/CSV na JSON-replay binnen dezelfde runtime. Een CI-controle herhaalt een echte WASM-browserexport in de desktophost en vergelijkt alle kolommen met expliciete toleranties (zie VALIDATION.md). Dit is geen bewijs van natuurkundige onderzoeksnauwkeurigheid; de aannames uit SCIENCE.md blijven gelden.
