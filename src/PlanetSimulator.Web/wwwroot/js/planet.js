import { createAppearance, setGeometry, drawAppearance, disposeAppearance } from './appearance.js';
// Presentation only. All simulation time and rotation arrive from the C# core.
let active;

const vertexSource = `#version 300 es
precision highp float;
in vec3 position;
in vec2 uv;
uniform mat4 viewProjection;
uniform float rotation;
out vec3 normal;
out vec2 coordinates;
void main() {
    float c = cos(rotation), s = sin(rotation);
    vec3 p = vec3(c * position.x + s * position.z, position.y, -s * position.x + c * position.z);
    normal = p;
    coordinates = uv;
    gl_Position = viewProjection * vec4(p, 1.0);
}`;
const fragmentSource = `#version 300 es
precision highp float;
in vec3 normal;
in vec2 coordinates;
uniform bool wireframe;
uniform bool temperatureMap;
uniform sampler2D temperatures;
uniform sampler2D reservoirs;
uniform bool surfaceModel;
uniform vec3 sunlight;
out vec4 color;
void main() {
    float band = 0.5 + 0.5 * sin(coordinates.x * 18.8495559 + coordinates.y * 5.12);
    vec3 surface = mix(vec3(0.373, 0.522, 0.6), vec3(0.651, 0.522, 0.376), band);
    vec2 gridCoordinates = coordinates * vec2(16.0, 8.0);
    vec2 edges = abs(fract(gridCoordinates - 0.5) - 0.5) / max(fwidth(gridCoordinates), vec2(0.0001));
    float grid = 1.0 - smoothstep(0.4, 1.2, min(edges.x, edges.y));
    surface = mix(surface, vec3(0.196, 0.298, 0.361), grid * 0.65);
    if (wireframe) surface = vec3(0.45, 0.8, 0.72);
    if (temperatureMap) {
        vec2 cellUv = vec2(coordinates.x, (1.0 - cos(coordinates.y * 3.14159265)) * 0.5);
        float value = clamp((texture(temperatures, cellUv).r - 170.0) / 160.0, 0.0, 1.0) * 4.0;
        vec3 a = vec3(39.,74.,142.) / 255., b = vec3(59.,171.,193.) / 255.;
        vec3 c = vec3(122.,203.,164.) / 255., d = vec3(239.,196.,101.) / 255., e = vec3(217.,88.,73.) / 255.;
        vec3 heat = value < 1. ? mix(a,b,value) : value < 2. ? mix(b,c,value-1.) : value < 3. ? mix(c,d,value-2.) : mix(d,e,value-3.);
        vec2 cells = cellUv * vec2(24.,12.);
        vec2 borders = abs(fract(cells - 0.5) - 0.5) / max(fwidth(cells), vec2(0.0001));
        heat *= 1.0 - 0.25 * (1.0 - smoothstep(0.3, 1.0, min(borders.x, borders.y)));
        color = vec4(heat, 1.0);
        return;
    }
    if (surfaceModel) {
        vec2 cellUv = vec2(coordinates.x, (1.0 - cos(coordinates.y * 3.14159265)) * 0.5);
        vec2 water = texture(reservoirs, cellUv).rg;
        surface = water.r == 0.0 ? vec3(118.,137.,80.) / 255.
            : mix(vec3(25.,89.,145.) / 255., vec3(225.,239.,242.) / 255., (water.r > 0. ? water.g / water.r : 0.));
    }
    float light = max(dot(normalize(normal), normalize(sunlight)), 0.0);
    color = vec4(surface * (0.09 + light * 0.91), 1.0);
}`;

export function createSphere(segments = 96, rings = 48) {
    const vertices = [], triangles = [], lines = [];
    for (let ring = 0; ring <= rings; ring++) {
        const latitude = Math.PI * ring / rings;
        for (let segment = 0; segment <= segments; segment++) {
            const longitude = Math.PI * 2 * segment / segments;
            vertices.push(Math.sin(latitude) * Math.cos(longitude), Math.cos(latitude),
                Math.sin(latitude) * Math.sin(longitude), segment / segments, ring / rings);
        }
    }
    for (let ring = 0; ring < rings; ring++) {
        for (let segment = 0; segment < segments; segment++) {
            const a = ring * (segments + 1) + segment, b = a + segments + 1;
            triangles.push(a, b, a + 1, a + 1, b, b + 1);
            lines.push(a, b, a, a + 1);
        }
    }
    return { vertices: new Float32Array(vertices), triangles: new Uint16Array(triangles), lines: new Uint16Array(lines) };
}

function compile(gl, type, source) {
    const shader = gl.createShader(type);
    gl.shaderSource(shader, source);
    gl.compileShader(shader);
    if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) {
        const message = gl.getShaderInfoLog(shader);
        gl.deleteShader(shader);
        throw new Error(`WebGL shader: ${message}`);
    }
    return shader;
}

export function cameraMatrix(yaw, pitch, distance, aspect) {
    // Column-major projection * look-at, with the camera looking at the origin.
    const z = [Math.cos(pitch) * Math.sin(yaw), Math.sin(pitch), Math.cos(pitch) * Math.cos(yaw)];
    const x = [Math.cos(yaw), 0, -Math.sin(yaw)];
    const y = [-Math.sin(pitch) * Math.sin(yaw), Math.cos(pitch), -Math.sin(pitch) * Math.cos(yaw)];
    const view = [x[0], y[0], z[0], 0, x[1], y[1], z[1], 0, x[2], y[2], z[2], 0, 0, 0, -distance, 1];
    const f = 1 / Math.tan(Math.PI / 8), near = 0.01, far = 100;
    const projection = [f / aspect, 0, 0, 0, 0, f, 0, 0, 0, 0, (far + near) / (near - far), -1,
        0, 0, 2 * far * near / (near - far), 0];
    const result = new Float32Array(16);
    for (let column = 0; column < 4; column++) {
        for (let row = 0; row < 4; row++) {
            for (let k = 0; k < 4; k++) result[column * 4 + row] += projection[k * 4 + row] * view[column * 4 + k];
        }
    }
    return result;
}

export async function start(canvas, reference, geometry) {
    stop();
    const gl = canvas.getContext('webgl2', { antialias: true, alpha: true });
    if (!gl) throw new Error('WebGL 2 is unavailable.');
    const vertex = compile(gl, gl.VERTEX_SHADER, vertexSource);
    const fragment = compile(gl, gl.FRAGMENT_SHADER, fragmentSource);
    const program = gl.createProgram();
    gl.attachShader(program, vertex);
    gl.attachShader(program, fragment);
    gl.linkProgram(program);
    gl.deleteShader(vertex);
    gl.deleteShader(fragment);
    if (!gl.getProgramParameter(program, gl.LINK_STATUS)) throw new Error(`WebGL link: ${gl.getProgramInfoLog(program)}`);
    gl.useProgram(program);
    const sphere = createSphere();
    const vao = gl.createVertexArray();
    gl.bindVertexArray(vao);
    const vertexBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ARRAY_BUFFER, vertexBuffer);
    gl.bufferData(gl.ARRAY_BUFFER, sphere.vertices, gl.STATIC_DRAW);
    const position = gl.getAttribLocation(program, 'position'), uv = gl.getAttribLocation(program, 'uv');
    gl.enableVertexAttribArray(position);
    gl.vertexAttribPointer(position, 3, gl.FLOAT, false, 20, 0);
    gl.enableVertexAttribArray(uv);
    gl.vertexAttribPointer(uv, 2, gl.FLOAT, false, 20, 12);
    const triangleBuffer = gl.createBuffer(), lineBuffer = gl.createBuffer();
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, triangleBuffer);
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, sphere.triangles, gl.STATIC_DRAW);
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, lineBuffer);
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, sphere.lines, gl.STATIC_DRAW);
    const temperatureTexture = gl.createTexture();
    gl.bindTexture(gl.TEXTURE_2D, temperatureTexture);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.R32F, 24, 12, 0, gl.RED, gl.FLOAT, new Float32Array(288).fill(230));
    const reservoirTexture = gl.createTexture();
    gl.bindTexture(gl.TEXTURE_2D, reservoirTexture);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MIN_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_MAG_FILTER, gl.NEAREST);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_S, gl.REPEAT);
    gl.texParameteri(gl.TEXTURE_2D, gl.TEXTURE_WRAP_T, gl.CLAMP_TO_EDGE);
    gl.texImage2D(gl.TEXTURE_2D, 0, gl.RG32F, 24, 12, 0, gl.RG, gl.FLOAT, new Float32Array(576));
    gl.uniform1i(gl.getUniformLocation(program, 'reservoirs'), 1);
    const controller = new AbortController();
    const options = { signal: controller.signal };
    const state = { gl, program, canvas, reference, vao, vertexBuffer, triangleBuffer, lineBuffer, controller,
        reservoirTexture, surfaceModel: false, surfaceLocation: gl.getUniformLocation(program, 'surfaceModel'),
        temperatureTexture, regional: false, temperatureMap: true, declination: 0, sphere, yaw: 0.5, pitch: 0.25, distance: 3.5, rotation: 0, wireframe: false, stopped: false,
        pending: false, lastTick: performance.now(), frame: 0, drag: null,
        matrixLocation: gl.getUniformLocation(program, 'viewProjection'),
        rotationLocation: gl.getUniformLocation(program, 'rotation'),
        mapLocation: gl.getUniformLocation(program, 'temperatureMap'),
        sunlightLocation: gl.getUniformLocation(program, 'sunlight'),
        wireframeLocation: gl.getUniformLocation(program, 'wireframe') };
    state.appearance = createAppearance(gl, sphere);
    setGeometry(state.appearance, geometry);
    state.temperatureMap = false;
    state.atmosphere = true;
    state.relief = 1;
    active = state;
    canvas.addEventListener('pointerdown', event => {
        if (event.button !== 0) return;
        state.drag = { id: event.pointerId, x: event.clientX, y: event.clientY };
        canvas.setPointerCapture(event.pointerId);
    }, options);
    canvas.addEventListener('pointermove', event => {
        if (!state.drag || state.drag.id !== event.pointerId) return;
        state.yaw -= (event.clientX - state.drag.x) * 0.008;
        state.pitch = Math.max(-1.45, Math.min(1.45, state.pitch + (event.clientY - state.drag.y) * 0.008));
        state.drag.x = event.clientX;
        state.drag.y = event.clientY;
    }, options);
    canvas.addEventListener('pointerup', () => { state.drag = null; }, options);
    canvas.addEventListener('pointercancel', () => { state.drag = null; }, options);
    canvas.addEventListener('wheel', event => {
        event.preventDefault();
        const delta = event.deltaY * (event.deltaMode === 1 ? 16 : event.deltaMode === 2 ? canvas.clientHeight : 1);
        state.distance = Math.max(1.4, Math.min(12, state.distance + delta * 0.003));
    }, { ...options, passive: false });
    canvas.addEventListener('webglcontextlost', event => {
        event.preventDefault();
        state.stopped = true;
        showError('De grafische verbinding is onderbroken. Vernieuw de pagina om verder te gaan.');
    }, options);
    document.addEventListener('visibilitychange', () => { state.lastTick = performance.now(); }, options);
    const snapshot = await reference.invokeMethodAsync('Advance', 0);
    updateSnapshot(state, snapshot);
    state.frame = requestAnimationFrame(now => animate(state, now));
    fetch(new URL('build.json', document.baseURI)).then(response => {
        if (!response.ok) return null;
        return response.json();
    }).then(build => {
        const label = document.getElementById('build-version');
        if (label && build) label.textContent = build.commit === 'local' ? 'Milestone 6 · lokaal' : `Milestone 6 · ${build.commit.slice(0, 7)}`;
    }).catch(() => {});
}

function updateSnapshot(state, snapshot) {
    state.rotation = snapshot.rotationRadians;
    state.regional = !!snapshot.regional;
    state.surfaceModel = !!snapshot.regional?.surface;
    if (state.surfaceModel) {
        const surface = snapshot.regional.surface;
        const field = new Float32Array(576);
        for (let i = 0; i < 288; i++) {
            field[2 * i] = surface.waterMassPerSquareMeter[i];
            field[2 * i + 1] = surface.iceMassPerSquareMeter[i];
        }
        state.gl.activeTexture(state.gl.TEXTURE1);
        state.gl.bindTexture(state.gl.TEXTURE_2D, state.reservoirTexture);
        state.gl.texSubImage2D(state.gl.TEXTURE_2D, 0, 0, 0, 24, 12, state.gl.RG, state.gl.FLOAT, field);
        state.gl.activeTexture(state.gl.TEXTURE0);
    }
    if (snapshot.regional) {
        state.declination = snapshot.regional.solarDeclinationRadians;
        state.gl.bindTexture(state.gl.TEXTURE_2D, state.temperatureTexture);
        state.gl.texSubImage2D(state.gl.TEXTURE_2D, 0, 0, 0, 24, 12, state.gl.RED, state.gl.FLOAT,
            new Float32Array(snapshot.regional.temperaturesKelvin));
    }
}

export function setSurfaceGeometry(data) { if (active) setGeometry(active.appearance, data); }
export function setAtmosphere(value) { if (active) active.atmosphere = value; }
export function setRelief(value) { if (active) active.relief = value; }
export function setTemperatureMap(value) { if (active) active.temperatureMap = value; }

function showError(message) {
    const element = document.getElementById('blazor-error-ui');
    if (element) {
        element.textContent = message;
        element.style.display = 'block';
    }
}

function animate(state, now) {
    if (state.stopped) return;
    if (!document.hidden && !state.pending && now - state.lastTick >= 100) {
        const seconds = Math.min(60, (now - state.lastTick) / 1000);
        state.lastTick = now;
        state.pending = true;
        state.reference.invokeMethodAsync('Advance', seconds).then(snapshot => {
            if (!state.stopped) updateSnapshot(state, snapshot);
        }).catch(() => {
            if (!state.stopped) {
                state.stopped = true;
                showError('De simulatie is onderbroken. Vernieuw de pagina om opnieuw te starten.');
            }
        }).finally(() => { state.pending = false; });
    }
    draw(state);
    state.frame = requestAnimationFrame(next => animate(state, next));
}

function draw(state) {
    const { gl, canvas } = state;
    const ratio = Math.min(window.devicePixelRatio || 1, 2);
    const width = Math.max(1, Math.round(canvas.clientWidth * ratio));
    const height = Math.max(1, Math.round(canvas.clientHeight * ratio));
    if (canvas.width !== width || canvas.height !== height) {
        canvas.width = width;
        canvas.height = height;
    }
    gl.viewport(0, 0, width, height);
    gl.clearColor(0, 0, 0, 0);
    gl.clear(gl.COLOR_BUFFER_BIT | gl.DEPTH_BUFFER_BIT);
    gl.enable(gl.DEPTH_TEST);
    if (state.surfaceModel && !state.temperatureMap) {
        const camera = [Math.cos(state.pitch) * Math.sin(state.yaw) * state.distance, Math.sin(state.pitch) * state.distance,
            Math.cos(state.pitch) * Math.cos(state.yaw) * state.distance];
        const rendered = drawAppearance(state.appearance, {
            matrix: cameraMatrix(state.yaw, state.pitch, state.distance, width / height), camera,
            sunlight: [Math.cos(state.declination), Math.sin(state.declination), 0], rotation: state.rotation,
            relief: state.relief, atmosphere: state.atmosphere, wireframe: state.wireframe, reservoirs: state.reservoirTexture
        });
        if (rendered) return;
    }
    gl.useProgram(state.program);
    gl.bindVertexArray(state.vao);
    gl.uniformMatrix4fv(state.matrixLocation, false, cameraMatrix(state.yaw, state.pitch, state.distance, width / height));
    gl.uniform1f(state.rotationLocation, state.rotation);
    gl.uniform1i(state.wireframeLocation, state.wireframe ? 1 : 0);
    gl.uniform1i(state.surfaceLocation, state.surfaceModel ? 1 : 0);
    gl.uniform1i(state.mapLocation, state.regional && state.temperatureMap ? 1 : 0);
    gl.uniform3f(state.sunlightLocation, state.regional ? Math.cos(state.declination) : 1,
        state.regional ? Math.sin(state.declination) : 0.4, state.regional ? 0 : 0.5);
    gl.activeTexture(gl.TEXTURE1);
    gl.bindTexture(gl.TEXTURE_2D, state.reservoirTexture);
    gl.activeTexture(gl.TEXTURE0);
    gl.bindTexture(gl.TEXTURE_2D, state.temperatureTexture);
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, state.wireframe ? state.lineBuffer : state.triangleBuffer);
    gl.drawElements(state.wireframe ? gl.LINES : gl.TRIANGLES,
        state.wireframe ? state.sphere.lines.length : state.sphere.triangles.length, gl.UNSIGNED_SHORT, 0);
}

export function resetCamera() {
    if (!active) return;
    active.yaw = 0.5;
    active.pitch = 0.25;
    active.distance = 3.5;
}

export function setWireframe(value) {
    if (active) active.wireframe = value;
}

export function stop() {
    if (!active) return;
    const state = active;
    active = null;
    state.stopped = true;
    cancelAnimationFrame(state.frame);
    state.controller.abort();
    state.gl.deleteBuffer(state.vertexBuffer);
    state.gl.deleteBuffer(state.triangleBuffer);
    state.gl.deleteBuffer(state.lineBuffer);
    state.gl.deleteVertexArray(state.vao);
    state.gl.deleteTexture(state.temperatureTexture);
    state.gl.deleteTexture(state.reservoirTexture);
    disposeAppearance(state.appearance);
    state.gl.deleteProgram(state.program);
}
