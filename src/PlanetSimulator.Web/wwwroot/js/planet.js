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
out vec4 color;
void main() {
    float band = 0.5 + 0.5 * sin(coordinates.x * 18.8495559 + coordinates.y * 5.12);
    vec3 surface = mix(vec3(0.373, 0.522, 0.6), vec3(0.651, 0.522, 0.376), band);
    vec2 gridCoordinates = coordinates * vec2(16.0, 8.0);
    vec2 edges = abs(fract(gridCoordinates - 0.5) - 0.5) / max(fwidth(gridCoordinates), vec2(0.0001));
    float grid = 1.0 - smoothstep(0.4, 1.2, min(edges.x, edges.y));
    surface = mix(surface, vec3(0.196, 0.298, 0.361), grid * 0.65);
    if (wireframe) surface = vec3(0.45, 0.8, 0.72);
    float light = max(dot(normalize(normal), normalize(vec3(1.0, 0.4, 0.5))), 0.0);
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

export async function start(canvas, reference) {
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
    const controller = new AbortController();
    const options = { signal: controller.signal };
    const state = { gl, program, canvas, reference, vao, vertexBuffer, triangleBuffer, lineBuffer, controller,
        sphere, yaw: 0.5, pitch: 0.25, distance: 3.5, rotation: 0, wireframe: false, stopped: false,
        pending: false, lastTick: performance.now(), frame: 0, drag: null,
        matrixLocation: gl.getUniformLocation(program, 'viewProjection'),
        rotationLocation: gl.getUniformLocation(program, 'rotation'),
        wireframeLocation: gl.getUniformLocation(program, 'wireframe') };
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
    state.rotation = snapshot.rotationRadians;
    state.frame = requestAnimationFrame(now => animate(state, now));
    fetch(new URL('build.json', document.baseURI)).then(response => {
        if (!response.ok) return null;
        return response.json();
    }).then(build => {
        const label = document.getElementById('build-version');
        if (label && build) label.textContent = build.commit === 'local' ? 'Milestone 1.5 · lokaal' : `Milestone 1.5 · ${build.commit.slice(0, 7)}`;
    }).catch(() => {});
}

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
            if (!state.stopped) state.rotation = snapshot.rotationRadians;
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
    gl.useProgram(state.program);
    gl.bindVertexArray(state.vao);
    gl.uniformMatrix4fv(state.matrixLocation, false, cameraMatrix(state.yaw, state.pitch, state.distance, width / height));
    gl.uniform1f(state.rotationLocation, state.rotation);
    gl.uniform1i(state.wireframeLocation, state.wireframe ? 1 : 0);
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
    state.gl.deleteProgram(state.program);
}
