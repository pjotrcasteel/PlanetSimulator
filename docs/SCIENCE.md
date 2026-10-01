# Milestone 2: globale stralingsbalans

## Vergelijkingen en eenheden

Het model beschrijft een uniform lichaam met één temperatuur. De relatieve helderheid van de ster is l; de afstand r wordt uitgedrukt in astronomische eenheden. De referentie-instraling op 1 AU is 1361 W/m².

- Instraling loodrecht op de stralen: S = 1361 l / r² [W/m²].
- Gemiddeld geabsorbeerd vermogen: Q = S (1 − a) / 4 [W/m²].
- Uitgestraald vermogen: F = σ T⁴ [W/m²].
- Temperatuurverandering: C dT/dt = Q − F.
- Evenwichtstemperatuur: T_eq = (Q / σ)^(1/4).

De factor 1/4 volgt uit de onderschepte schijf πR² en het totale oppervlak 4πR². De straal valt daardoor uit deze per-oppervlaktebalans weg. We nemen een uniforme temperatuur aan; dat is een modelvereenvoudiging en geen berekende lucht- of oceaancirculatie. Visuele belichting van de bol staat los van deze globale berekening.

σ = 5.670374419 × 10⁻⁸ W/m²/K⁴, afgerond op de weergegeven cijfers. a is Bond-albedo: de fractie van het totale invallende sterlicht die wordt gereflecteerd. Emissiviteit voor langgolvige warmtestraling is hier 1. Een opgelegde albedo van 0.3 maakt dit model nog geen planeet met aardse wolken.

## Referentiescenario

r = 1 AU, l = 1, a = 0.3 geeft Q = 238.175 W/m² en T_eq ≈ 254.578 K (−18.572 °C). NASA beschrijft een aardse effectieve stralingstemperatuur van ongeveer 255 K. Dat is niet de huidige gemiddelde oppervlaktetemperatuur; een atmosfeer en broeikaseffect ontbreken hier.

Het scenario start op 230 K. De warmtecapaciteit C = 10⁷ J/m²/K is een expliciete scenarioaanname, geen gemeten totale warmtecapaciteit van de aarde en geen klimaatkalibratie. Daardoor is ook de opwarmtijd geen voorspelling voor de aarde. Geen temperatuur wordt kunstmatig naar het evenwicht getrokken: de differentiaalvergelijking bepaalt de beweging.

## Numeriek model

Vierde-orde Runge–Kutta, vaste simulatietijdstappen van 60 seconden. Het energiebudget gebruikt dezelfde gewogen warmtestraling als de integrator. Opgenomen en uitgestraalde energie worden opgeteld met gecompenseerde sommatie. De opgeslagen warmteverandering C(T − T_initial) wordt vergeleken met cumulatieve opname minus uitstraling.

Tests controleren het analytische evenwicht, de inverse kwadratenwet, warmen en koelen vanuit verschillende begintemperaturen, stabiel evenwicht, energiebalans na een gewijzigde forcing, kleinere tijdstappen en onafhankelijkheid van renderfrequentie. Energiebalans is een numerieke controle, geen bewijs van realistische klimaatvoorspellingen.

Het geldigheidsgebied voor deze implementatie is begrensd: afstand 0.2–5 AU, relatieve sterhelderheid 0–2, albedo 0–1, begintemperatuur 1–1000 K en C van 10⁵ tot 10¹⁰ J/m²/K. Deze implementatiegrenzen begrenzen de numerieke scenario’s; ze zijn geen universele natuurkundige limieten. De UI biedt afstand 0.5–2 AU en albedo 0–0.8.

## Experimenten en beperkingen

Afstand en albedo wijzigen de forcing zonder de temperatuur of bestaande energievoorraad te herschrijven. Reset zet temperatuur en energiebudget terug naar de beginconditie, met behoud van de gekozen forcing. Een verandering van afstand via een schuifregelaar is een scenario-experiment: kosten en baanmechanica van het verplaatsen van een planeet worden nog niet gesimuleerd.

Geen atmosfeer, gaschemie, broeikaseffect, water, faseovergangen, ijs-albedo-feedback, geothermie, seizoenen, regionale temperaturen of biologische processen. Geen aardse bewoonbaarheidsclaim op basis van één temperatuur. Dat volgt pas als de betreffende systemen zijn toegevoegd en gevalideerd.

## Primaire bronnen

- NASA, energiebudget en de geometrische verdeling van zoninstraling: https://science.nasa.gov/earth/earth-observatory/climate-and-earths-energy-budget/
- NASA, referentie-instraling 1361 W/m²: https://earth.gsfc.nasa.gov/climate/projects/solar-irradiance/science
- NASA, effectieve stralingstemperatuur circa 255 K: https://sunclimate.gsfc.nasa.gov/science
- NIST, CODATA 2022 Stefan–Boltzmann-constante: https://physics.nist.gov/cuu/pdf/wall_2022.pdf
