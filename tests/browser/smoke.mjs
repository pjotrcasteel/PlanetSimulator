import { chromium } from 'playwright';
import { createServer } from 'node:http';
import { readFile, mkdir } from 'node:fs/promises';
import { resolve, extname, sep } from 'node:path';
import assert from 'node:assert/strict';

const root = resolve(process.argv[2] || 'artifacts/web/wwwroot');
const types = { '.html': 'text/html', '.js': 'text/javascript', '.css': 'text/css', '.json': 'application/json',
    '.wasm': 'application/wasm', '.png': 'image/png', '.dat': 'application/octet-stream' };
const server = createServer(async (request, response) => {
    try {
        const url = new URL(request.url, 'http://localhost');
        if (!url.pathname.startsWith('/PlanetSimulator/')) throw new Error('Wrong subpath');
        const file = resolve(root, decodeURIComponent(url.pathname.slice('/PlanetSimulator/'.length)) || 'index.html');
        if (!file.startsWith(root + sep)) throw new Error('Invalid path');
        const bytes = await readFile(file);
        response.writeHead(200, { 'Content-Type': types[extname(file)] || 'application/octet-stream' });
        response.end(bytes);
    } catch {
        response.writeHead(404);
        response.end();
    }
});
await new Promise(resolve => server.listen(0, '127.0.0.1', resolve));
let page;
const browser = await chromium.launch({ executablePath: process.env.PLANET_CHROMIUM_PATH || undefined, args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
try {
    page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => { if (response.status() >= 400) errors.push(`${response.status()} ${response.url()}`); });
    await page.goto(`http://127.0.0.1:${server.address().port}/PlanetSimulator/`);
    await page.waitForFunction(() => document.querySelector('#pause')?.disabled === false, null, { timeout: 60000 });
    await page.selectOption('#climate-model', 'global');
    await page.locator('#pause').click();
    await page.waitForFunction(() => document.querySelector('#pause').textContent.includes('Hervatten'));
    // Allow the last in-flight bridge call to settle before comparing pause values.
    await page.waitForTimeout(250);
    const paused = await page.locator('#elapsed-days').innerText();
    await page.waitForTimeout(400);
    assert.equal(await page.locator('#elapsed-days').innerText(), paused);
    await page.selectOption('#speed', '86400');
    await page.locator('#pause').click();
    await page.waitForFunction(previous => parseFloat(document.querySelector('#elapsed-days').textContent) > previous + 0.2,
        parseFloat(paused));
    await page.locator('#pause').click();
    await page.locator('#reset').click();
    await page.waitForFunction(() => parseFloat(document.querySelector('#elapsed-days').textContent) === 0);
    assert.equal(Number(await page.locator('#temperature').getAttribute('data-kelvin')), 230);
    const referenceEquilibrium = Number(await page.locator('#equilibrium-temperature').getAttribute('data-kelvin'));
    assert.ok(Math.abs(referenceEquilibrium - 254.578) < 0.001);
    async function setRange(id, value) {
        await page.locator(id).evaluate((element, next) => {
            element.value = next;
            element.dispatchEvent(new Event('input', { bubbles: true }));
        }, value);
    }
    await setRange('#star-distance', '1.5');
    await setRange('#albedo', '0.6');
    await page.waitForFunction(() => Number(document.querySelector('#equilibrium-temperature').dataset.kelvin) < 200);
    assert.equal(Number(await page.locator('#temperature').getAttribute('data-kelvin')), 230);
    assert.ok(parseFloat(await page.locator('#net-flux').innerText()) < 0);
    assert.equal(await page.locator('#thermal-trend').innerText(), 'Koelt af');
    await page.selectOption('#speed', '604800');
    await page.locator('#pause').click();
    await page.waitForFunction(() => Number(document.querySelector('#temperature').dataset.kelvin) < 229);
    await page.locator('#pause').click();
    await page.locator('#reset').click();
    await page.waitForFunction(() => Number(document.querySelector('#temperature').dataset.kelvin) === 230);
    await setRange('#star-distance', '1');
    await setRange('#albedo', '0.3');
    await page.waitForFunction(() => Number(document.querySelector('#equilibrium-temperature').dataset.kelvin) > 254);
    assert.equal(Number(await page.locator('#temperature').getAttribute('data-kelvin')), 230);
    // Run, download, reload and replay a scenario through the actual trimmed WASM app.
    await page.locator('#scenario-name').fill('Browserreferentie');
    await page.locator('#experiment-days').fill('60');
    await page.locator('#run-experiment').click();
    await page.waitForFunction(() => document.querySelector('#sample-count')?.textContent.includes('61'), null, { timeout: 60000 });
    assert.equal(await page.locator('#result-name').innerText(), 'Browserreferentie');
    const baselineFinal = Number(await page.locator('#sample-temperature').getAttribute('data-kelvin'));
    assert.ok(baselineFinal > 250 && baselineFinal < 255);
    assert.equal(await page.locator('#temperature-curve').getAttribute('data-samples'), '61');
    async function downloadText(selector) {
        const pending = page.waitForEvent('download');
        await page.locator(selector).click();
        return await readFile(await (await pending).path(), 'utf8');
    }
    const scenarioJson = await downloadText('#export-scenario');
    const baselineCsv = await downloadText('#export-csv');
    const scenario = JSON.parse(scenarioJson);
    assert.equal(scenario.formatVersion, 1);
    assert.equal(scenario.modelVersion, 'global-blackbody-rk4-60s-v1');
    assert.equal(scenario.climate.arealHeatCapacity, 1e7);
    assert.equal(baselineCsv.trim().split('\n').length, 62);
    await page.locator('#save-scenario').click();
    await page.locator('#scenario-name').fill('Veranderd');
    await page.locator('#load-scenario').click();
    await page.waitForFunction(() => document.querySelector('#scenario-name').value === 'Browserreferentie');
    await page.locator('#scenario-name').fill('Afkoeling op dag 30');
    await page.locator('#add-change').click();
    await page.locator('#change-day-0').fill('30');
    await page.locator('#change-distance-0').fill('1.5');
    await page.locator('#change-albedo-0').fill('0.6');
    await page.locator('#run-experiment').click();
    await page.waitForFunction(() => document.querySelector('#result-name')?.textContent === 'Afkoeling op dag 30', null, { timeout: 60000 });
    assert.equal(await page.locator('#previous-curve').count(), 1);
    assert.ok(Number(await page.locator('#sample-temperature').getAttribute('data-kelvin')) < baselineFinal - 20);
    await setRange('#inspect-day', '30');
    await page.waitForFunction(() => document.querySelector('#inspected-day').textContent === '30');
    const changedCsv = await downloadText('#export-csv');
    const changedRows = changedCsv.trim().split('\n');
    const baselineRows = baselineCsv.trim().split('\n');
    assert.equal(changedRows[31].split(',')[1], baselineRows[31].split(',')[1]);
    assert.ok(Number(changedRows[31].split(',')[2]) < 200);
    const incompatible = { ...scenario, modelVersion: 'future-model' };
    await page.locator('#import-scenario').setInputFiles({ name: 'incompatible.json', mimeType: 'application/json', buffer: Buffer.from(JSON.stringify(incompatible)) });
    await page.waitForFunction(() => document.querySelector('#experiment-message').textContent.includes('Onbekende'));
    assert.equal(await page.locator('#scenario-name').inputValue(), 'Afkoeling op dag 30');
    await page.locator('#import-scenario').setInputFiles({ name: 'scenario.json', mimeType: 'application/json', buffer: Buffer.from(scenarioJson) });
    await page.waitForFunction(() => document.querySelector('#scenario-name').value === 'Browserreferentie');
    assert.equal(await page.locator('.change-row').count(), 0);
    await page.locator('#run-experiment').click();
    await page.waitForFunction(() => document.querySelector('#result-name')?.textContent === 'Browserreferentie', null, { timeout: 60000 });
    assert.equal(await downloadText('#export-csv'), baselineCsv);
    // Browser storage survives a full page reload; no run is started by loading.
    await page.reload();
    await page.waitForFunction(() => document.querySelector('#pause')?.disabled === false, null, { timeout: 60000 });
    await page.selectOption('#climate-model', 'global');
    await page.locator('#pause').click();
    await page.locator('#load-scenario').click();
    await page.waitForFunction(() => document.querySelector('#scenario-name').value === 'Browserreferentie');
    assert.equal(await page.locator('#temperature-chart').count(), 0);
    await page.locator('#run-experiment').click();
    await page.waitForFunction(() => document.querySelector('#sample-count')?.textContent.includes('61'), null, { timeout: 60000 });
    assert.equal(await downloadText('#export-csv'), baselineCsv);
    await mkdir('artifacts/browser', { recursive: true });
    const { writeFile } = await import('node:fs/promises');
    await writeFile('artifacts/browser/replay-scenario.json', scenarioJson);
    await writeFile('artifacts/browser/replay-results.csv', baselineCsv);

    // Spatial science and the temperature texture use the same model as regional replay.
    await page.selectOption('#climate-model', 'regional');
    await page.waitForFunction(() => document.querySelector('#regional-summary')?.dataset.cells === '288');
    assert.equal(Number(await page.locator('#regional-min').getAttribute('data-kelvin')), 230);
    assert.equal(Number(await page.locator('#regional-max').getAttribute('data-kelvin')), 230);
    await setRange('#axial-tilt', '45');
    await setRange('#heat-diffusion', '1.2');
    assert.equal(Number(await page.locator('#temperature').getAttribute('data-kelvin')), 230);
    assert.equal(await page.locator('#speed option[value="604800"]').isDisabled(), true);
    await page.selectOption('#speed', '86400');
    await page.locator('#pause').click();
    await page.waitForFunction(() => parseFloat(document.querySelector('#elapsed-days').textContent) > 3, null, { timeout: 60000 });
    await page.locator('#pause').click();
    await page.waitForTimeout(250);
    const minimum = Number(await page.locator('#regional-min').getAttribute('data-kelvin'));
    const maximum = Number(await page.locator('#regional-max').getAttribute('data-kelvin'));
    assert.ok(maximum - minimum > 1);
    assert.ok(Math.abs(parseFloat(await page.locator('#regional-budget').innerText())) < 0.02);
    const mapped = await page.locator('#planet-canvas').screenshot();
    await page.locator('#temperature-map').uncheck();
    await page.waitForTimeout(150);
    const decorative = await page.locator('#planet-canvas').screenshot();
    assert.notDeepEqual(mapped, decorative);
    await page.locator('#temperature-map').check();
    await page.locator('#reset').click();
    assert.equal(Number(await page.locator('#regional-min').getAttribute('data-kelvin')), 230);
    assert.equal(await page.locator('#axial-tilt').inputValue(), '45');
    assert.equal(await page.locator('#heat-diffusion').inputValue(), '1.2');
    await page.locator('#scenario-name').fill('Regionale referentie');
    await page.locator('#experiment-days').fill('3');
    await page.selectOption('#experiment-model', 'regional');
    await page.waitForFunction(() => document.querySelector('#experiment-tilt') !== null);
    await page.locator('#experiment-tilt').fill('45');
    await page.locator('#experiment-diffusion').fill('1.2');
    await page.locator('#run-experiment').click();
    await page.waitForFunction(() => document.querySelector('#result-name')?.textContent === 'Regionale referentie', null, { timeout: 60000 });
    assert.equal(await page.locator('#experiment-regional-range').getAttribute('data-cells'), '288');
    const regionalJson = await downloadText('#export-run-scenario');
    const regionalCsv = await downloadText('#export-csv');
    const regionsCsv = await downloadText('#export-regions');
    assert.equal(JSON.parse(regionalJson).modelVersion, 'regional-blackbody-rk4-60s-12x24-v1');
    assert.equal(regionsCsv.trim().split('\n').length, 289);
    assert.ok(regionalCsv.includes('north_mean_K'));
    await page.locator('#import-scenario').setInputFiles({ name: 'regional.json', mimeType: 'application/json', buffer: Buffer.from(regionalJson) });
    await page.waitForFunction(() => document.querySelector('#scenario-name').value === 'Regionale referentie');
    await page.locator('#run-experiment').click();
    await page.waitForFunction(() => document.querySelector('#run-experiment').disabled === false, null, { timeout: 60000 });
    assert.equal(await downloadText('#export-csv'), regionalCsv);
    assert.equal(await downloadText('#export-regions'), regionsCsv);
    await writeFile('artifacts/browser/regional-scenario.json', regionalJson);
    await writeFile('artifacts/browser/regional-results.csv', regionalCsv);
    await writeFile('artifacts/browser/regional-regions.csv', regionsCsv);
    // Water phase state, surface rendering and reproducible enthalpy experiments.
    await page.selectOption('#climate-model', 'surface');
    await page.waitForFunction(() => document.querySelector('#surface-summary') !== null);
    assert.equal(Number(await page.locator('#surface-summary').getAttribute('data-liquid-fraction')), 0);
    await page.locator('#temperature-map').uncheck();
    await page.waitForTimeout(250);
    await page.screenshot({ path: 'artifacts/browser/surface.png', fullPage: true });
    await page.selectOption('#experiment-model', 'surface');
    await page.locator('#scenario-name').fill('Waterfase referentie');
    await page.locator('#experiment-days').fill('2');
    await page.locator('#experiment-relief').fill('0');
    await page.locator('#experiment-water').fill('0.01');
    await page.locator('#initial-temperature').fill('273.15');
    await page.locator('#stellar-luminosity').fill('2');
    await page.locator('#run-experiment').click();
    await page.waitForFunction(() => document.querySelector('#result-name')?.textContent === 'Waterfase referentie', null, { timeout: 60000 });
    assert.ok(Number(await page.locator('#experiment-water-summary').getAttribute('data-liquid-fraction')) > 0);
    const waterJson = await downloadText('#export-run-scenario');
    const waterCsv = await downloadText('#export-csv');
    const waterRegions = await downloadText('#export-regions');
    assert.equal(JSON.parse(waterJson).modelVersion, 'surface-enthalpy-rk4-60s-12x24-v1');
    assert.ok(waterRegions.includes('ice_kg_m2'));
    await page.locator('#import-scenario').setInputFiles({ name: 'water.json', mimeType: 'application/json', buffer: Buffer.from(waterJson) });
    await page.waitForFunction(() => document.querySelector('#experiment-model').value === 'surface');
    await page.locator('#run-experiment').click();
    await page.waitForFunction(() => document.querySelector('#run-experiment').disabled === false, null, { timeout: 60000 });
    assert.equal(await downloadText('#export-csv'), waterCsv);
    assert.equal(await downloadText('#export-regions'), waterRegions);
    await writeFile('artifacts/browser/water-scenario.json', waterJson);
    await writeFile('artifacts/browser/water-results.csv', waterCsv);
    await writeFile('artifacts/browser/water-regions.csv', waterRegions);
    // Presentation settings must never alter time, temperature or the water budget.
    await page.locator('#warm-start').click();
    await page.waitForFunction(() => Number(document.querySelector('#temperature').dataset.kelvin) === 285);
    await page.waitForTimeout(300);
    const warm = await page.locator('#planet-canvas').screenshot();
    await page.evaluate(() => window.scrollTo(0, 0));
    await page.waitForTimeout(150);
    await page.screenshot({ path: 'artifacts/browser/planet-warm.png', fullPage: true });
    await page.locator('#atmosphere').uncheck();
    await page.waitForTimeout(200);
    const noAtmosphere = await page.locator('#planet-canvas').screenshot();
    assert.notDeepEqual(warm, noAtmosphere);
    await setRange('#visual-relief', '0');
    await page.waitForTimeout(200);
    const noRelief = await page.locator('#planet-canvas').screenshot();
    assert.notDeepEqual(noAtmosphere, noRelief);
    assert.equal(Number(await page.locator('#temperature').getAttribute('data-kelvin')), 285);
    assert.equal(parseFloat(await page.locator('#elapsed-days').innerText()), 0);
    assert.equal(Number(await page.locator('#surface-summary').getAttribute('data-liquid-fraction')), 1);
    assert.equal(await page.evaluate(() => document.querySelector('#planet-canvas').getContext('webgl2').getError()), 0);
    await page.locator('#atmosphere').check();
    await setRange('#visual-relief', '1');
    await page.locator('#cold-start').click();
    await page.waitForFunction(() => Number(document.querySelector('#temperature').dataset.kelvin) === 230);
    await page.waitForTimeout(250);
    const cold = await page.locator('#planet-canvas').screenshot();
    assert.notDeepEqual(warm, cold);
    await page.evaluate(() => window.scrollTo(0, 0));
    await page.waitForTimeout(150);
    await page.screenshot({ path: 'artifacts/browser/planet-cold.png', fullPage: true });
    await page.selectOption('#climate-model', 'regional');
    await page.locator('#temperature-map').check();
    // Both new models must replay through JSON and export every budget column.
    for (const [choice, identity] of [['hydrology', 'hydrology-rk4-60s-12x24-v1'], ['chemistry', 'atmosphere-chemistry-rk4-60s-12x24-v1'], ['biology', 'microbial-ch2o-rk4-60s-12x24-v1'], ['terraforming', 'terraforming-inventory-rk4-60s-12x24-v1']]) {
        await page.selectOption('#climate-model', choice);
        await page.waitForFunction(() => document.querySelector('#water-cycle-summary') !== null);
        if (choice === 'chemistry') {
            assert.ok(Math.abs(Number(await page.locator('#atmosphere-summary').getAttribute('data-co2-pressure')) - 40.53) < 1e-6);
        }
        if (choice === 'biology') {
            await page.locator('#biology-summary').waitFor();
            await page.locator('#warm-start').click();
            assert.ok(Number(await page.locator('#biology-summary').getAttribute('data-biomass')) > 0);
        }
        await page.selectOption('#experiment-model', choice);
        if (choice === 'terraforming') {
            await page.locator('#terraform-summary').waitFor();
            await page.locator('#terraform-editor').waitFor();
            await page.locator('#engineering-energy').fill('90000000');
        }
        await page.locator('#experiment-days').fill('2');
        await page.locator('#initial-temperature').fill('285');
        await page.locator('#experiment-water').fill('3');
        await page.locator('#run-experiment').click();
        await page.waitForFunction(() => document.querySelector('#sample-count')?.textContent.includes('3 meetpunten'), null, { timeout: 60000 });
        await page.locator('#run-experiment').waitFor({ state: 'visible' });
        await page.waitForFunction(() => !document.querySelector('#export-csv').disabled);
        const json = await downloadText('#export-run-scenario');
        const csv = await downloadText('#export-csv');
        const regions = await downloadText('#export-regions');
        assert.equal(JSON.parse(json).modelVersion, identity);
        assert.ok(csv.includes('water_budget_error_kg'));
        assert.ok(regions.includes('vapor_kg_m2'));
        if (choice === 'chemistry') {
            assert.ok(csv.includes('carbon_budget_error_kg'));
            assert.ok(regions.includes('co2_partial_pressure_pa'));
            await page.screenshot({ path: 'artifacts/browser/chemistry.png', fullPage: true });
        }
        if (choice === 'biology') {
            assert.ok(csv.includes('phosphorus_budget_error_kg'));
            assert.ok(regions.includes('biomass_kg_m2'));
            await page.locator('#experiment-biology-summary').waitFor();
            const rows = csv.trim().split('\n').map(line => line.split(','));
            const biomassColumn = rows[0].indexOf('biomass_kg');
            assert.ok(Number(rows.at(-1)[biomassColumn]) > Number(rows[1][biomassColumn]));
            await page.screenshot({ path: 'artifacts/browser/biology.png', fullPage: true });
        }
        if (choice === 'terraforming') {
            assert.equal(JSON.parse(json).atmosphere.terraforming.initialEnergyJoulesPerSquareMeter, 90000000);
            assert.ok(csv.includes('logistics_budget_error_kg'));
            assert.ok(regions.includes('transit_co2_kg_m2'));
            await page.locator('#experiment-terraform-summary').waitFor();
            const data = csv.trim().split('\n').map(line => line.split(','));
            assert.ok(Number(data.at(-1)[data[0].indexOf('captured_co2_kg')]) > 0);
            assert.ok(Number(data.at(-1)[data[0].indexOf('delivered_co2_kg')]) > 0);
            await page.screenshot({ path: 'artifacts/browser/terraforming.png', fullPage: true });
        }
        await page.locator('#import-scenario').setInputFiles({ name: choice + '.json', mimeType: 'application/json', buffer: Buffer.from(json) });
        await page.waitForFunction(expected => document.querySelector('#experiment-model').value === expected, choice);
        await page.locator('#run-experiment').click();
        await page.waitForFunction(() => !document.querySelector('#export-csv').disabled);
        assert.equal(await downloadText('#export-csv'), csv);
        assert.equal(await downloadText('#export-regions'), regions);
        await writeFile('artifacts/browser/' + choice + '-scenario.json', json);
        await writeFile('artifacts/browser/' + choice + '-results.csv', csv);
        await writeFile('artifacts/browser/' + choice + '-regions.csv', regions);
    }
    await page.selectOption('#climate-model', 'regional');
    // Keep a developed spatial field visible in the final desktop and mobile previews.
    await page.locator('#pause').click();
    await page.waitForFunction(() => parseFloat(document.querySelector('#elapsed-days').textContent) > 3, null, { timeout: 60000 });
    await page.locator('#pause').click();
    await page.waitForTimeout(250);
    await page.screenshot({ path: 'artifacts/browser/desktop.png', fullPage: true });
    await page.locator('#wireframe').check();
    await page.waitForTimeout(150);
    await page.screenshot({ path: 'artifacts/browser/wireframe.png', fullPage: true });
    await page.locator('#wireframe').uncheck();
    const canvas = page.locator('#planet-canvas');
    const box = await canvas.boundingBox();
    await page.mouse.move(box.x + box.width / 2, box.y + box.height / 2);
    await page.mouse.down();
    await page.mouse.move(box.x + box.width / 2 + 100, box.y + box.height / 2 + 40, { steps: 10 });
    await page.mouse.up();
    await page.mouse.wheel(0, -100);
    await page.setViewportSize({ width: 390, height: 844 });
    await page.waitForTimeout(150);
    await page.screenshot({ path: 'artifacts/browser/mobile.png', fullPage: true });
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth), false);
    assert.equal(await page.locator('.error').count(), 0);
    assert.deepEqual(errors, []);
    console.log('Published Blazor app: climate, scenarios, scheduled changes, comparison, spherical climate, tilt, conservative budgets, regional replay, JSON/CSV replay, browser storage, WebGL and mobile layout passed.');
} catch (error) {
    await mkdir('artifacts/browser', { recursive: true });
    if (page) await page.screenshot({ path: 'artifacts/browser/failure.png', fullPage: true }).catch(() => {});
    throw error;
} finally {
    await browser.close();
    await new Promise(resolve => server.close(resolve));
}
