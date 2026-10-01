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

# Milestone 4: regionaal energiebalansmodel

## Rooster en instraling

12 breedterijen × 24 lengtecellen = 288 cellen. Grenzen liggen gelijkmatig in μ = sin(φ) en λ. Iedere cel heeft exact dezelfde analytische oppervlakte R² Δμ Δλ; alle cellen bedekken 4πR². De centrale breedte is asin(μ_midden). De celvorm wordt richting de pool smaller in lengte en groter in breedte. Dit is een grof klimatologisch rooster, geen terreinrooster.

Cel-instraling: q_i = S (1−a) max(0, n_i · s), met normaal n_i en sterrichting s. De centrale puntwaarden worden per tijdstip met één factor genormaliseerd, zodat hun oppervlaktegemiddelde exact S(1−a)/4 is. Dit bewaart onderschepte energie maar is geen exacte integratie van iedere afzonderlijke cel, vooral bij dag/nachtgrenzen en poolkappen. De donkere celcentra blijven donker; de schemergrens kan door een cel lopen. Hogere ruimtelijke resolutie en betere celquadratuur blijven toekomstige verfijningen.

Voorgeschreven zonnedeclinatie: δ = asin(sin(ε) sin(2πt/Y)), met ashelling ε en jaarlengte Y. Dag 0 is equinox; Y/4 noordelijk zomermaximum; 3Y/4 zuidelijk zomermaximum. Een voorgeschreven zonnedag duurt 86400 seconden. De renderer gebruikt dezelfde draaiing en declinatie voor de verlichtingsrichting. De rotatieas blijft visueel verticaal; veranderende sterdeclinatie is dezelfde relatieve hoek voor de instraling. Geen baanellips, precessie, Kepler-koppeling tussen jaarlengte en afstand of veranderende sterafstand gedurende de jaarcyclus. Afstand en jaarlengte zijn hier onafhankelijke experimentparameters.

## Warmtetransport en integratie

C dT_i/dt = q_i − σ T_i⁴ + transport_i.

We benaderen de sferische angular diffusieoperator met eindige volumes:

D [∂_μ((1−μ²)∂_μ T) + (1/(1−μ²)) ∂²_λ T].

D heeft eenheid W/m²/K en vertegenwoordigt effectief horizontaal warmtetransport. Het is geen gemeten materiaalgeleiding en geen berekende atmosferische/oceanische stroming. De standaard D = 0.6 is een scenarioaanname, geen aardse kalibratie. Een gebruikelijke meridionale energiebalansbenadering gebruikt dezelfde sferische operator in de breedterichting; onze toepassing voegt de lengterichting toe.

Per buurvlak wordt één flux berekend en met gelijke tegengestelde tekens geboekt. Lengterichting is periodiek; bij de polen is de meridionale grensflux nul. Noord/zuid-conductantie per cel is D(1−μ_grens²)/Δμ²; oost/west D/((1−μ_midden²)Δλ²). Intern transport maakt zo geen globale energie. De geometrische R²-factor valt uit de per-oppervlaktevergelijking weg; D is een effectieve angular klimaatcoëfficiënt.

RK4 met stappen van 60 seconden; sterpositie wordt ook op de tussenstadia berekend. Geen temperatuurclamping of kunstmatig evenwichtsdoel. De energiebudgetten gebruiken dezelfde RK4-gewogen emissie als de temperatuurberekening, en gecompenseerde sommatie. Warmtevoorraadverandering per planeetoppervlakte is C(T_gemiddeld − T_begin). Het residu wordt getoond in J/m².

Het globale gemiddelde wordt berekend over gelijke oppervlaktes. Uitstraling is het gemiddelde van σT_i⁴, niet σ maal het vierde vermogen van de gemiddelde temperatuur. De stralingsequivalente temperatuur (Q/σ)^(1/4) is daardoor geen algemeen evenwichtsdoel voor de gemiddelde celtemperatuur. Een regionale planeet kan in globale stralingsbalans zijn terwijl regio’s blijven reageren op dag/nacht en seizoenen.

## Bereik en validatie

Het klimaatbereik uit milestone 2 blijft gelden. D = 0–2 W/m²/K, ε = 0–90°, jaarlengte 30–1000 dagen. Het regionale model gebruikt bewust alleen dit 12×24-rooster; de vaste tijdstap en D-grens worden niet automatisch geldig bij een veel fijner rooster. Live hosts begrenzen regionale tijdsnelheid op één dag per seconde; scenario’s blijven vaste simulatietijdstappen gebruiken.

Tests: roosteroppervlakten (relatieve tolerantie 1e-12), lengte-naad en poolindexen, exact totaal stervermogen, equinoxsymmetrie, dagrotatie, seizoens- en poolnachtgedrag bij 45° ashelling, behoud van transport, contrastafname, volledig energiebudget na wijzigingen, 60/30-secondenconvergentie bij de hoge-instraling/lage-C/hoog-D-grens, frame-onafhankelijkheid, reset en JSON/CSV-replay van iedere cel. De test gebruikt voor tijdstapvergelijking maximaal 0.02 K afwijking; dit is geen nauwkeurigheidsclaim voor echte klimaatgegevens. Spatial discretisatieconvergentie van de gekoppelde oplossing wordt nog niet geclaimd.

Grof rooster, geen atmosfeer of broeikaseffect, wolken, land/oceanen, latente warmte, wind, water of biologische processen. Temperatuurkleuren zijn diagnostiek en geen terrein. Er is nog geen gevalideerde aardse klimaatvoorspelling of bewoonbaarheidsgrens.

## Aanvullende primaire bronnen

- NASA, ashelling en seizoenen: https://science.nasa.gov/earth/facts/
- CLIMLAB documentatie, sferische meridionale diffusie, operator en D-eenheid: https://climlab.readthedocs.io/en/latest/api/climlab.dynamics.MeridionalHeatDiffusion.html
