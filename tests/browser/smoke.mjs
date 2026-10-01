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
const browser = await chromium.launch({ args: ['--use-gl=angle', '--use-angle=swiftshader', '--enable-unsafe-swiftshader'] });
try {
    const page = await browser.newPage({ viewport: { width: 1440, height: 1000 } });
    const errors = [];
    page.on('pageerror', error => errors.push(error.message));
    page.on('response', response => { if (response.status() >= 400) errors.push(`${response.status()} ${response.url()}`); });
    await page.goto(`http://127.0.0.1:${server.address().port}/PlanetSimulator/`);
    await page.waitForFunction(() => document.querySelector('#pause')?.disabled === false, null, { timeout: 60000 });
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
    await mkdir('artifacts/browser', { recursive: true });
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
    console.log('Published Blazor app: subpath, WebGL start, pause, speed, reset, wireframe, camera and mobile layout passed.');
} finally {
    await browser.close();
    await new Promise(resolve => server.close(resolve));
}
