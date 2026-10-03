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


## Milestone 5: terrein, water en ijs

Het nieuwe model `surface-enthalpy-rk4-60s-12x24-v1` voegt een synthetische, deterministische hoogtekaart en zoetwaterkolommen toe. Het bestaande regionale model blijft reproduceerbaar. Reliëf is een som van sferische sinusfuncties: geen aardse topografie, erosie of tektoniek. De bol blijft geometrisch glad; werkelijk terrein en betere oceaanweergave volgen bij milestone 6.

Bij initialisatie wordt één waterniveau gevonden met bisectie zodat `mean(max(0, niveau − hoogte))` gelijk is aan de opgegeven globale waterinventaris in meters. Referentiedichtheid: 1000 kg/m³. Iedere cel bewaart daarna haar volledige massa. De standaard 10 m reliëf en 3 m inventaris geven ondiepe reservoirs om veranderingen zichtbaar te maken. Diepe kolommen kunnen worden ingesteld, maar reageren zeer traag: het hele reservoir deelt één temperatuur, zonder stratificatie.

Energie per oppervlakte wordt als enthalpie H geïntegreerd met dezelfde RK4-stappen van maximaal 60 s. Met substraatcapaciteit C, watermassa m en smeltpunt Tm = 273,15 K geldt:

- H < 0: T = Tm + H/(C + m c_ice), alles ijs.
- 0 ≤ H ≤ m L: T = Tm, vloeibare fractie H/(m L).
- H > m L: T = Tm + (H − m L)/(C + m c_water), alles vloeibaar.

Bij exact Tm begint water als ijs. Constante, afgeronde waarden: L = 333.500 J/kg, c_ice = 2.100 J/kg/K en c_water = 4.180 J/kg/K. Dit zijn benaderingen rond het vriespunt, geen volledige toestandsvergelijking. Referenties voor nauwkeurige eigenschappen: [NIST: Properties of Ice and Supercooled Water](https://www.nist.gov/publications/properties-ice-and-supercooled-water), [NIST water-tabellen](https://www.nist.gov/publications/thermodynamic-properties-water-tabulation-iapws-formulation-1995-thermodynamic) en [IAPWS smelt- en sublimatiecurven](https://iapws.org/technical-guidance/release/MeltSub).

Instraling, emissie en intern transport gebruiken de temperatuur van iedere RK4-enthalpiestap. Intern transport blijft paarsgewijs conservatief. Het energiebudget vergelijkt de verandering in gemiddelde H met geïntegreerde instraling minus emissie; latente warmte wordt dus niet toegevoegd of weggegooid door een temperatuurcorrectie. Waterdiagnostiek toont totale, vloeibare en ijsmassa; beide fasen tellen op tot het vaste reservoir.

Geldigheidsgebied: een pedagogisch zoetwatermodel met voorgeschreven vaste druk rond het smeltpunt. Het model berekent geen druk, atmosfeer, saliniteit, verdamping, sublimatie, koken, afvoer of ijsexpansie. Vloeibaar water op een atmosfeerloze echte planeet wordt hiermee niet voorspeld. Buiten het gebied rond freezing zijn de constante materiaalwaarden een sterke vereenvoudiging. Grote temperaturen geven geen realistische waterfase. Bond-albedo blijft uniform en onafhankelijk van ijs: de wit/blauwe kaart is geen nieuwe reflectieparameter. Werkelijke ijsbedekking kan niet uit één kolomtemperatuur worden afgeleid; de kaart mengt wit/blauw volgens ijsmassafractie. Waterbedekking betekent cellen met een reservoir, inclusief bevroren reservoirs.


## Milestone 6: reconstructie en optische weergave

De natuurkundige integrator en scenario-identiteiten blijven gelijk aan milestone 5. De continue hoogtefunctie is gedeeld met `PlanetSimulator.Rendering`. De 288 klimaatcellen blijven de rekeneenheden; 18.721 visuele vertices en 36.864 driehoeken geven een fijnere weergave. Dezelfde C#-geometrie wordt naar MonoGame en WebGL gestuurd. Met nul waterinventaris worden geen oceaanmaterialen getekend.

Kustlijnen volgen de continue hoogtefunctie en het berekende beginniveau. De zichtbare oppervlakteverdeling kan daardoor afwijken van het grove celoppervlaktepercentage. Deze interpolatie is geen hogere wetenschappelijke resolutie. De weergave leest de toestand alleen. Voor fasefracties worden totale watermassa en ijsmassa afzonderlijk bilineair geïnterpoleerd op het gelijke-area-rooster, pas daarna wordt hun verhouding genomen; dit vermijdt onterechte verdunning door droge buurcellen.

Droog land krijgt deterministisch ruisdetail en verhoogde richels, begrensd op 4% van de visuele straal. Het reliëf is dus sterk uitvergroot ten opzichte van de standaard 10 m. De schuif verandert geometrie en normalen, geen hoogtedata, zwaartekracht, waterniveau of energie. Bruine/grijze grondkleuren zijn een materiaalkeuze en geen bodemchemie; er wordt geen vegetatie verondersteld. IJskorrels zijn decoratief; de ijsmassafractie komt uit enthalpie. Het wateroppervlak gebruikt Fresnel-achtige hoekafhankelijkheid en een Blinn-Phong-zonreflectie. Er is geen golfdynamica of stroming.

De atmosfeer is een optische demonstratie met exponentieel afnemende dichtheid (schaalhoogte 0,012 visuele stralen), buitengrens 1,065 en een Rayleigh-achtige fasefactor 0,75(1+cos²θ). Twaalf samples langs de zichtstraal geven een benaderde optische diepte. Samples in de schaduw van de eenheidsbol dragen niet bij. Blauw verstrooit sterker dan rood. Inspiratie en verdere fysische uitwerking: [NVIDIA GPU Gems 2, hoofdstuk 16](https://developer.nvidia.com/gpugems/gpugems2/part-ii-shading-lighting-and-shadows/chapter-16-accurate-atmospheric-scattering).

Dit is een vereenvoudigde additieve enkelvoudige verstrooiing, zonder volledige extinctie, meervoudige verstrooiing, aerosolen, wolken of terugkoppeling naar de energiebalans. De optische schaal is gekozen voor zichtbaarheid, niet afgeleid uit een gasinventaris. Druk en chemie blijven bij milestone 8 horen. De desktop berekent belichting en atmosfeer op vertices; WebGL doet de belichting per fragment. De geometrie en toestandsgegevens zijn gelijk, maar screenshots hoeven niet pixelidentiek te zijn.

## Milestone 7: waterkringloop

Damp, vloeibare wolken en oppervlaktewater worden in kg/m² bijgehouden op hetzelfde 12×24-rooster. De beginvoorraad damp/wolken wordt uit lokaal oppervlaktewater onttrokken, begrensd door beschikbaar water. Een droge wereld krijgt dus geen water cadeau. De opgegeven begintemperatuur geldt na deze verdeling.

Verdamping, condensatie en neerslag gebruiken begrensde relaxatiefluxen. De effectieve verzadigde kolom is 50 kg/m² bij 288,15 K met een exponentiële temperatuurparameterisatie (schaal 17 K). Dit is geen berekende relatieve vochtigheid of verticaal profiel. De voorgeschreven wind is 12 m/s oostwaarts en 2 m/s zuidwaarts; poolgrenzen zijn gesloten. Sequentiële eerste-ordetransfers dragen massa én opgeslagen enthalpie. Afvoer verplaatst uitsluitend vloeibaar water naar een buurcel met lager wateroppervlak; een hydrostatisch gevuld bekken loopt niet kunstmatig leeg.

De enthalpiereferentie is ijs bij 273,15 K. Damp bevat smeltwarmte plus 2.500.300 J/kg verdampingswarmte en benaderde voelbare warmte (1.850 J/kg/K). Wolken dragen vloeistofenthalpie; condensatiewarmte gaat naar het oppervlak. Neerslag en afvoer dragen hun eigen enthalpie. De globale energiebalans telt oppervlakte- en atmosferische waterenthalpie samen. Referentie voor verdampingswarmte rond het tripelpunt: [NIST](https://srd.nist.gov/jpcrdreprint/1.555947.pdf).

Beperkingen: geen verticale luchtenergie, drukafhankelijke waterfasen, ijswolken, sublimatie, koken of dynamisch berekende wind. De stap is operator-split en eerste orde voor transport; de thermische RK4-stap maakt het geheel niet vierde orde. De 60/30/15-secondenproef onderzoekt temperatuur en dampconvergentie. De renderergeometrie gebruikt nog de beginkustlijn; de reservoirkaart toont actuele celmassa’s, maar is geen volledig bewegend wateroppervlak.

## Milestone 8: droge atmosfeer en CO₂-uitwisseling

De vier droge gassen zijn N₂, O₂, CO₂ en Ar. Voor de totale droge druk geldt p = g Σmᵢ. Molfractie xᵢ = (mᵢ/Mᵢ) / Σ(mⱼ/Mⱼ); partiële druk pᵢ = xᵢp. Voor een gemengde atmosfeer is mᵢg dus niet de partiële druk. Beginmassa’s worden uit totale druk, molfracties en molaire massa’s berekend. De standaard 400 ppm CO₂ bij 101.325 Pa geeft 40,53 Pa CO₂-druk.

Druk is hier droge gasdiagnostiek. Waterdamp en wolkengewicht worden niet opgeteld; er is geen verticale structuur, drukgedreven menging, ontsnapping naar de ruimte of broeikas-terugkoppeling. De optische gloed is onafhankelijk. De C/O/N-budgetten omvatten deze droge gassen en de opgeloste/korst-CO₂, niet alle atomen in gesteenten en water.

CO₂(g) ↔ CO₂(aq) gebruikt de verdunde Henry-relatie c = Hp met H = 3,4×10⁻⁴ mol/(m³ Pa) bij 298,15 K en factor exp[2400 K × (1/T − 1/298,15 K)]. De waterhoeveelheid bepaalt het oplosvolume. T wordt voor deze benadering begrensd tot 273,15–323,15 K; buiten dit bereik wordt geen gevalideerde oplosbaarheid geclaimd. Kou verhoogt de oplosbaarheid. Gas en oplossing worden samen naar een massabehoudend evenwicht gerelaxeerd, met standaard tijdschaal 24 uur. Bij bevriezen of droogvallen komt CO₂ weer vrij.

De eindige korstvoorraad bevat CO₂-equivalente massa; uitgassing is een voorgeschreven bronflux met een maximum gelijk aan de resterende voorraad, geen geochemische reactievoorspelling. Standaard is deze flux nul. Alle overdrachten verplaatsen hele CO₂-moleculen zodat C en O behouden blijven. N₂/O₂/Ar reageren niet. Opgelost CO₂ blijft een lokaal reservoir; afvoer transporteert nog geen opgeloste stoffen. Oplossingswarmte, pH, carbonaatchemie, biologie en fotochemie ontbreken.

Bronnen: [NASA: hydrostatische luchtdruk](https://www.grc.nasa.gov/www/k-12/airplane/atmosphere.html), [NOAA: definitie CO₂-partiële druk](https://www.ncei.noaa.gov/access/ocean-carbon-acidification-data-system/oceans/Handbook_2007/sop05.pdf), [Sander: Henry-constanten voor CO₂, versie 5](https://henrys-law.org/henry/casrn/124-38-9).

De warme start is geen garantie op langdurig vloeibaar water of bewoonbaarheid. Met de referentie-instraling en zonder broeikaseffect kan deze wereld weer bevriezen.


## Milestone 9 — één microbieel compartiment

De biologische toestand is een CH₂O-equivalent per cel, plus een afzonderlijke fosforquota. De nettoreactie is CO₂ + H₂O + licht → CH₂O + O₂; aerobe ademhaling keert deze reactie om. Zie [OpenStax Biology 2e, fotosynthese](https://openstax.org/books/biology-2e/pages/8-1-overview-of-photosynthesis) en [koolstoffixatie en ademhaling](https://openstax.org/books/biology-2e/pages/8-3-using-light-energy-to-make-organic-molecules) voor het biologische principe. De onderstaande parameters zijn expliciete modelkeuzes, geen soortspecifieke kalibratie.

- Inoculum: standaard 0,001 kg CH₂O/m² in cellen met initieel water, ook als dit ijs is. Het is een expliciet toegevoegde beginvoorraad, met eigen koolstof, gebonden water en chemische energie. Geen spontane vorming van leven. De zaadhoeveelheid wordt begrensd door de totale fosforquota.
- Fosfor: standaard 0,0001 kg P/m²; 0,030974/106 kg P per mol CH₂O als vaste, illustratieve quota. Vrij en gebonden P blijven samen constant. P is een nutriëntentracer; fosfaatreacties, stikstofassimilatie en zuur-basechemie worden niet voorgesteld.
- Activiteit: driehoek tussen 273,15 en 313,15 K met optimum 293,15 K, uitsluitend bij vloeibaar water. Buiten dit bereik blijft biomassa slapend; er is nog geen sterfte, detritus, verspreiding of evolutie. Dit is dus geen universele temperatuurgrens voor leven.
- Groei: maximaal 1/dag maal activiteit. De feitelijke stap is het minimum van deze potentiële groei, beschikbare opgeloste CO₂, vloeibaar water, vrij P, maximaal 2% van lokaal geabsorbeerde lichtenergie en beschikbaar positief oppervlakte-enthalpie. CO₂ komt via het bestaande gas-water-uitwisselingsmodel binnen.
- Ademhaling: maximaal 0,03/dag maal activiteit, exact exponentieel begrensd en beperkt tot beschikbare O₂. O₂ wordt rechtstreeks uit de lokale gaskolom genomen; zuurstofoplossing en diffusie in water zijn nog niet gemodelleerd. Ademhaling recycleert P en water en geeft CO₂ terug aan het opgeloste reservoir.
- Energie: 467 kJ/mol CH₂O is een vaste effectieve opslagwaarde. Fotosynthese trekt deze energie af van het reeds opgewarmde oppervlak; ademhaling geeft die terug. Het totale budget bevat thermische, atmosferische water- en chemische energie. Er komt geen extra energie bij de lichtbron. Biomassa heeft nog geen eigen voelbare warmtecapaciteit.
- Water: één mol water wordt per mol gevormd CH₂O gebonden. De totale waterdiagnostiek telt het **water-equivalent in biomassa** mee; dit is geen vloeibaar water. Waterstofbehoud volgt deze equivalente waterbalans. Het droge C-budget bevat de verandering van de werkelijk opgeslagen biologische koolstof. O in water en CH₂O valt paarsgewijs weg, zodat het bestaande CO₂/O₂-zuurstofbudget blijft sluiten. N₂ is inert.

De biologie verandert de gasvoorraden en chemische energie, maar nog niet albedo, broeikasopaciteit of de visuele terreinkleuren. Geen bomen, dieren of ecosystemen; geen bewijs dat de wereld duurzaam bewoonbaar is. Het model blijft een transparant, numeriek controleerbaar eerste compartiment.


## Milestone 10 — fysieke kosten en CO₂-logistiek

Een installatie vertegenwoordigt een regionaal netwerk over één volledige rekencel, geen enkel gebouw. Vermogen, grondstoffen, tankcapaciteit en voorraden zijn **per m² van die cel**; globale totals gebruiken de echte celoppervlakte. De grote energie- en materiaalhoeveelheden zijn daardoor zichtbaar in de uitlezing. Er is geen fictieve geldscore of automatische toevoer van gratis energie.

De beginvoorraad opgeslagen energie is extern ingebracht vóór t=0. Ze telt mee in de totale energiebalans. Bouw trekt eenmaal materiaal en energie af; het materiaal blijft als gebouwde infrastructuur aanwezig. Bouwenergie wordt bij aanvang als warmte afgegeven, een vereenvoudiging van de bouwfase. Na de ingestelde bouwtijd werken installaties uitsluitend binnen hun start-/stopvenster. Als een regio onvoldoende bouwmateriaal of energie heeft, start de bouw niet. Installaties delen lokale voorraden in scenario-volgorde; die volgorde is dus een expliciete prioriteitsregel. Er is geen mijnbouw, herladen of transport van bouwmateriaal/elektriciteit in deze versie.

- **Verwarmer:** omzetting van eindige opgeslagen energie naar oppervlakte-enthalpie, begrensd door vermogen × actieve tijd. Smelten volgt daarna de bestaande latente-warmteberekening. Er wordt geen water toegevoegd.
- **CO₂-afvang:** verplaatst hele CO₂-moleculen uit de lokale droge atmosfeer naar een eindige tank. Productie is begrensd door gasvoorraad, tankruimte, debiet en beschikbare energie. De pompenergie wordt warmte; scheidingschemie, gaswarmtecapaciteit, drukvatmechanica en opgeslagen compressie-exergie zijn nog niet gemodelleerd.
- **Transport:** neemt CO₂ uit de lokale tank en plaatst het in een zending met expliciete aankomsttijd. Ontvangst voegt exact die massa aan de doelatmosfeer toe. Reeds vertrokken lading arriveert ook als de installatie is gestopt of leeggelopen. De reisduur is een scenario-invoer; de afstand volgt de grote cirkel tussen celmiddens en verhoogt de energiekosten: verwerking J/kg + afstand × transport J/kg/m. Reistijd en afstand zijn nog geen voertuig- of leidingmodel. Alle transportenergie wordt als warmte in de bronregio geboekt.

De standaardwaarden (100 W/m² vermogen, 0,1 kg CO₂/m²/dag, 2 MJ/kg verwerking, 0,1 J/kg/m transport, 0,25 dag bouw/reis, 2 kg/m² bouwmateriaal en 1 MJ/m² bouwenergie) zijn **illustratieve technische aannames**, geen gemeten installatieprestaties. Energievoorraad: 100 MJ/m² per regio; bouwvoorraad: 10 kg/m²; tank: 1 kg CO₂/m². Het voorbeeld verwarmt en vangt af in cel 144 en levert gas aan cel 145. Zestien installaties is de maximale scenariogrootte.

Behoud: beschikbare + gebouwde materialen blijven constant; beschikbare + verbruikte energie blijven constant. Thermische energie plus chemische energie, hydrologische energie en resterende technische energie sluiten samen tegen de bestaande zon-/stralingsbalans. Afgevangen CO₂ = tanks + onderweg + afgeleverd. Tanks en zendingen blijven tevens in de planetaire C/O-budgetten. De temperaturen worden na technische warmte-invoer opnieuw uit enthalpie bepaald. Dit is een expliciet gesplitste stap; de heaterproef vergelijkt 60, 30 en 15 seconden.

CO₂-afvang verandert druk en beschikbaarheid voor chemie/biologie, maar **nog niet het broeikaseffect**. Een verwarmer kan een regio opwarmen zolang hij energie heeft; daarmee is geen duurzame bewoonbaarheid aangetoond. Visuele gebouwen, volledige industrie, transportnetwerken en kalibratie volgen eventueel later.
