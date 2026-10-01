// Shared C# terrain geometry; GPU presentation only. Reservoirs remain in the simulation.
const terrainVertex = `#version 300 es
precision highp float;
in vec3 position;
in vec3 surfaceNormal;
in vec3 albedo;
in vec2 uv;
in float water;
uniform mat4 viewProjection;
uniform float rotation;
uniform float relief;
out vec3 worldPosition;
out vec3 normal;
out vec3 baseColor;
out vec2 coordinates;
out float waterMask;
void main() {
    float c = cos(rotation), s = sin(rotation);
    mat3 turn = mat3(c,0.,-s, 0.,1.,0., s,0.,c);
    vec3 radial = normalize(position);
    worldPosition = turn * radial * (1. + (length(position) - 1.) * relief);
    normal = turn * normalize(mix(radial, surfaceNormal, relief));
    baseColor = albedo;
    coordinates = uv;
    waterMask = water;
    gl_Position = viewProjection * vec4(worldPosition,1.);
}`;
const terrainFragment = `#version 300 es
precision highp float;
in vec3 worldPosition;
in vec3 normal;
in vec3 baseColor;
in vec2 coordinates;
in float waterMask;
uniform vec3 camera;
uniform vec3 sunlight;
uniform sampler2D reservoirs;
out vec4 color;
vec2 cell(ivec2 p) { return texelFetch(reservoirs, ivec2((p.x+24)%24,clamp(p.y,0,11)),0).rg; }
void main() {
    vec2 p = vec2(coordinates.x*24.-.5, clamp((1.-cos(coordinates.y*3.14159265359))*6.-.5,0.,11.));
    ivec2 a = ivec2(floor(p));
    vec2 f = fract(p);
    vec2 mass = mix(mix(cell(a),cell(a+ivec2(1,0)),f.x),mix(cell(a+ivec2(0,1)),cell(a+ivec2(1,1)),f.x),f.y);
    float ice = (mass.r > 0. ? clamp(mass.g/mass.r,0.,1.) : 0.) * waterMask;
    float liquid = waterMask*(1.-ice);
    vec3 radial = normalize(worldPosition), n = normalize(normal), v = normalize(camera-worldPosition);
    // Fine ice grain is decorative. Its presence and amount come from the phase state.
    vec3 body = vec3(sin(coordinates.y*3.14159265359)*cos(coordinates.x*6.28318530718),cos(coordinates.y*3.14159265359),
        sin(coordinates.y*3.14159265359)*sin(coordinates.x*6.28318530718));
    float grain = .92+.08*sin(body.x*179.+sin(body.z*113.)*3.)*sin(body.y*151.);
    vec3 base = mix(baseColor,vec3(.61,.75,.83)*grain,ice);
    float light = max(0.,dot(n,sunlight));
    vec3 value = base*(.018+1.35*light);
    vec3 halfway = (sunlight+v)/max(.000001,length(sunlight+v));
    float fresnel = .02+.98*pow(1.-max(0.,dot(n,v)),5.);
    float highlight = pow(max(0.,dot(n,halfway)),150.)*liquid*(.25+fresnel)*light*4.;
    value += vec3(1.,.91,.75)*highlight;
    value += vec3(.025,.065,.12)*liquid*fresnel*max(0.,dot(radial,sunlight));
    color = vec4(pow(clamp(value,0.,1.),vec3(1./2.2)),1.);
}`;
const airVertex = `#version 300 es
precision highp float;
in vec3 position;
uniform mat4 viewProjection;
out vec3 point;
void main() { point = position*1.065; gl_Position = viewProjection*vec4(point,1.); }`;
const airFragment = `#version 300 es
precision highp float;
in vec3 point;
uniform vec3 camera;
uniform vec3 sunlight;
out vec4 color;
void main() {
    vec3 direction = normalize(point-camera);
    float b = dot(camera,direction);
    float discriminant = b*b-dot(camera,camera)+1.065*1.065;
    if(discriminant<=0.) discard;
    float root = sqrt(discriminant), start = max(0.,-b-root), end = -b+root;
    float planet = b*b-dot(camera,camera)+1.;
    if(planet>0.) end = min(end,-b-sqrt(planet));
    float stepSize = max(0.,end-start)/12., depth = 0.;
    for(int i=0;i<12;i++) {
        vec3 samplePoint = camera+direction*(start+(float(i)+.5)*stepSize);
        float sunDot = dot(samplePoint,sunlight);
        if(sunDot<0. && sunDot*sunDot>dot(samplePoint,samplePoint)-1.) continue;
        depth += exp(-(length(samplePoint)-1.)/.012)*stepSize;
    }
    float cosine = dot(direction,sunlight), phase = .75*(1.+cosine*cosine);
    vec3 scatter = (1.-exp(-depth*vec3(3.8,8.5,18.)))*(.38*phase);
    color = vec4(scatter,1.);
}`;

function program(gl, vertexSource, fragmentSource) {
    const shaders = [gl.VERTEX_SHADER, gl.FRAGMENT_SHADER].map((type, index) => {
        const shader = gl.createShader(type);
        gl.shaderSource(shader, index === 0 ? vertexSource : fragmentSource);
        gl.compileShader(shader);
        if (!gl.getShaderParameter(shader, gl.COMPILE_STATUS)) throw new Error(gl.getShaderInfoLog(shader));
        return shader;
    });
    const result = gl.createProgram();
    shaders.forEach(shader => gl.attachShader(result, shader));
    gl.linkProgram(result);
    shaders.forEach(shader => gl.deleteShader(shader));
    if (!gl.getProgramParameter(result, gl.LINK_STATUS)) throw new Error(gl.getProgramInfoLog(result));
    return result;
}

function mesh(gl, shader, data, stride, attributes) {
    const vao = gl.createVertexArray(), vertices = gl.createBuffer(), indices = gl.createBuffer(), lines = gl.createBuffer();
    gl.bindVertexArray(vao);
    gl.bindBuffer(gl.ARRAY_BUFFER, vertices);
    gl.bufferData(gl.ARRAY_BUFFER, new Float32Array(data.vertices), gl.STATIC_DRAW);
    let offset = 0;
    for (const [name, size] of attributes) {
        const location = gl.getAttribLocation(shader, name);
        gl.enableVertexAttribArray(location);
        gl.vertexAttribPointer(location, size, gl.FLOAT, false, stride * 4, offset * 4);
        offset += size;
    }
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, indices);
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(data.indices), gl.STATIC_DRAW);
    const edges = [];
    for (let index = 0; index < data.indices.length; index += 3) {
        const [a,b,c] = data.indices.slice(index,index+3); edges.push(a,b,b,c,c,a);
    }
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, lines);
    gl.bufferData(gl.ELEMENT_ARRAY_BUFFER, new Uint16Array(edges), gl.STATIC_DRAW);
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, indices);
    return { vao, vertices, indices, lines, count: data.indices.length, lineCount: edges.length };
}

export function createAppearance(gl, sphere) {
    const terrain = program(gl, terrainVertex, terrainFragment), air = program(gl, airVertex, airFragment);
    const airMesh = mesh(gl, air, { vertices: sphere.vertices, indices: sphere.triangles }, 5, [['position', 3]]);
    const uniforms = shader => Object.fromEntries(['viewProjection', 'camera', 'sunlight', 'rotation', 'relief', 'reservoirs']
        .map(name => [name, gl.getUniformLocation(shader, name)]));
    return { gl, terrain, air, airMesh, terrainMesh: null, terrainUniforms: uniforms(terrain), airUniforms: uniforms(air) };
}

function disposeMesh(gl, value) {
    if (!value) return;
    gl.deleteBuffer(value.vertices); gl.deleteBuffer(value.indices); gl.deleteBuffer(value.lines); gl.deleteVertexArray(value.vao);
}

export function setGeometry(state, data) {
    const { gl } = state;
    disposeMesh(gl, state.terrainMesh);
    state.terrainMesh = data ? mesh(gl, state.terrain, data, 12,
        [['position', 3], ['surfaceNormal', 3], ['albedo', 3], ['uv', 2], ['water', 1]]) : null;
}

export function drawAppearance(state, frame) {
    const { gl, terrainMesh } = state;
    if (!terrainMesh) return false;
    const setup = (shader, uniforms, geometry) => {
        gl.useProgram(shader); gl.bindVertexArray(geometry.vao);
        gl.uniformMatrix4fv(uniforms.viewProjection, false, frame.matrix);
        gl.uniform3fv(uniforms.camera, frame.camera); gl.uniform3fv(uniforms.sunlight, frame.sunlight);
    };
    gl.disable(gl.CULL_FACE); gl.disable(gl.BLEND); gl.depthMask(true);
    setup(state.terrain, state.terrainUniforms, terrainMesh);
    gl.uniform1f(state.terrainUniforms.rotation, frame.rotation);
    gl.uniform1f(state.terrainUniforms.relief, frame.relief);
    gl.uniform1i(state.terrainUniforms.reservoirs, 1);
    gl.activeTexture(gl.TEXTURE1); gl.bindTexture(gl.TEXTURE_2D, frame.reservoirs);
    gl.bindBuffer(gl.ELEMENT_ARRAY_BUFFER, frame.wireframe ? terrainMesh.lines : terrainMesh.indices);
    gl.drawElements(frame.wireframe ? gl.LINES : gl.TRIANGLES, frame.wireframe ? terrainMesh.lineCount : terrainMesh.count, gl.UNSIGNED_SHORT, 0);
    if (frame.atmosphere && !frame.wireframe) {
        setup(state.air, state.airUniforms, state.airMesh);
        gl.enable(gl.BLEND); gl.blendFunc(gl.ONE, gl.ONE); gl.depthMask(false);
        gl.enable(gl.CULL_FACE); gl.frontFace(gl.CW); gl.cullFace(gl.BACK);
        gl.drawElements(gl.TRIANGLES, state.airMesh.count, gl.UNSIGNED_SHORT, 0);
        gl.disable(gl.CULL_FACE); gl.disable(gl.BLEND); gl.depthMask(true);
    }
    gl.activeTexture(gl.TEXTURE0);
    return true;
}

export function disposeAppearance(state) {
    disposeMesh(state.gl, state.terrainMesh); disposeMesh(state.gl, state.airMesh);
    state.gl.deleteProgram(state.terrain); state.gl.deleteProgram(state.air);
}
