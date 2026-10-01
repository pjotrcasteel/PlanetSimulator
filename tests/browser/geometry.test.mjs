import test from 'node:test';
import assert from 'node:assert/strict';
import { createSphere, cameraMatrix } from '../../src/PlanetSimulator.Web/wwwroot/js/planet.js';

test('mesh is a unit sphere with valid topology and wrapped longitude seam', () => {
    const segments = 96, rings = 48;
    const mesh = createSphere(segments, rings);
    const count = mesh.vertices.length / 5;
    assert.equal(count, (segments + 1) * (rings + 1));
    assert.equal(mesh.triangles.length, segments * rings * 6);
    for (let i = 0; i < count; i++) {
        const [x, y, z] = mesh.vertices.slice(i * 5, i * 5 + 3);
        assert.ok(Math.abs(Math.hypot(x, y, z) - 1) < 1e-6);
    }
    for (const index of [...mesh.triangles, ...mesh.lines]) assert.ok(index >= 0 && index < count);
    for (let ring = 0; ring <= rings; ring++) {
        const first = ring * (segments + 1) * 5, last = first + segments * 5;
        for (let axis = 0; axis < 3; axis++) assert.ok(Math.abs(mesh.vertices[first + axis] - mesh.vertices[last + axis]) < 1e-6);
    }
});

test('orbit camera keeps planet origin at the center for different views', () => {
    for (const pitch of [-1.45, 0, 1.45]) {
        for (const yaw of [-2, 0, 2]) {
            const matrix = cameraMatrix(yaw, pitch, 3.5, 16 / 9);
            assert.ok([...matrix].every(Number.isFinite));
            assert.equal(matrix[12], 0);
            assert.equal(matrix[13], 0);
            assert.equal(matrix[15], 3.5);
            assert.ok(Math.abs(matrix[14] / matrix[15]) < 1);
        }
    }
});
