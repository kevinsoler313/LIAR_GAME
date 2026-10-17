// LIAR GAME · Plantilla PS1 (lo-fi horror)
// 1) Títulos con la fuente Rye dibujados en un canvas diminuto y ampliados (pixelados).
// 2) Escena 3D: estás sentado a la mesa, frente al tahúr, bajo una sola lámpara.
//    El shader imita a la PlayStation 1: vértices que saltan a la grilla, texturas sin
//    corrección de perspectiva, luz por vértice, pocos colores con dithering y niebla.

(function () {
  "use strict";

  var reducirMovimiento = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  // ------------------------------------------------------------ Títulos pixelados

  function pixelarTitulo(el) {
    var texto = el.dataset.texto || el.textContent.trim();
    var lineas = texto.split("|");
    var alto = Number(el.dataset.px) || 16;      // alto de la letra en píxeles "reales"
    var escala = Number(el.dataset.escala) || 3;  // cuánto se amplía cada píxel
    var aberracion = el.dataset.aberracion === "1";
    var color = getComputedStyle(el).color;
    var fuente = alto + "px Rye";

    var medir = document.createElement("canvas").getContext("2d");
    medir.font = fuente;
    var ancho = 0;
    lineas.forEach(function (l) { ancho = Math.max(ancho, medir.measureText(l).width); });

    var altoLinea = Math.ceil(alto * 1.3);
    var lienzo = document.createElement("canvas");
    lienzo.width = Math.ceil(ancho) + 6;
    lienzo.height = altoLinea * lineas.length + 4;
    var ctx = lienzo.getContext("2d");
    ctx.font = fuente;
    ctx.textAlign = "center";
    ctx.textBaseline = "middle";

    lineas.forEach(function (l, i) {
      var x = lienzo.width / 2, y = 2 + altoLinea * i + altoLinea / 2;
      if (aberracion) {   // copias desplazadas en rojo y cian, como un CRT desajustado
        ctx.fillStyle = "rgba(255, 70, 20, 0.9)"; ctx.fillText(l, x - 1, y);
        ctx.fillStyle = "rgba(60, 200, 220, 0.6)"; ctx.fillText(l, x + 1, y);
      }
      ctx.fillStyle = color;
      ctx.fillText(l, x, y);
    });

    // Quitar el antialiasing: cada píxel es opaco o transparente
    var datos = ctx.getImageData(0, 0, lienzo.width, lienzo.height);
    for (var p = 3; p < datos.data.length; p += 4) datos.data[p] = datos.data[p] > 110 ? 255 : 0;
    ctx.putImageData(datos, 0, 0);

    lienzo.style.width = lienzo.width * escala + "px";
    lienzo.setAttribute("aria-hidden", "true");
    el.setAttribute("aria-label", lineas.join(" "));
    el.textContent = "";
    el.appendChild(lienzo);
  }

  function pixelarTodos() {
    document.querySelectorAll(".px-titulo").forEach(pixelarTitulo);
  }

  if (document.fonts && document.fonts.load) {
    Promise.race([
      document.fonts.load("16px Rye"),
      new Promise(function (r) { setTimeout(r, 2500); })
    ]).then(pixelarTodos, pixelarTodos);
  } else {
    pixelarTodos();
  }

  // ------------------------------------------------------------ Escena 3D

  var lienzo3D = document.getElementById("ps1-escena");
  if (!lienzo3D || !window.THREE) return;

  var ANCHO = 256, ALTO = 144;   // resolución interna, bien baja

  var renderer;
  try {
    renderer = new THREE.WebGLRenderer({ canvas: lienzo3D, antialias: false });
  } catch (e) {
    return;   // sin WebGL: queda el fondo negro
  }
  renderer.setPixelRatio(1);
  renderer.setSize(ANCHO, ALTO, false);
  renderer.setClearColor(0x050302);

  var escena = new THREE.Scene();
  var camara = new THREE.PerspectiveCamera(55, ANCHO / ALTO, 0.05, 30);

  // Texturas diminutas pintadas en un canvas
  function textura(w, h, pintar, repetir) {
    var c = document.createElement("canvas");
    c.width = w; c.height = h;
    pintar(c.getContext("2d"), w, h);
    var t = new THREE.CanvasTexture(c);
    t.magFilter = THREE.NearestFilter;
    t.minFilter = THREE.NearestFilter;
    t.generateMipmaps = false;
    if (repetir) t.wrapS = t.wrapT = THREE.RepeatWrapping;
    return t;
  }

  var texCarta = textura(16, 24, function (ctx, w, h) {
    for (var y = 0; y < h; y++)
      for (var x = 0; x < w; x++) {
        ctx.fillStyle = ((x + y) >> 1) % 2 ? "#3d2417" : "#241510";
        ctx.fillRect(x, y, 1, 1);
      }
    ctx.strokeStyle = "#8f733a"; ctx.lineWidth = 1; ctx.strokeRect(0.5, 0.5, w - 1, h - 1);
    ctx.strokeStyle = "#0b0705"; ctx.strokeRect(2.5, 2.5, w - 5, h - 5);
    ctx.fillStyle = "#d9661f"; ctx.fillRect(6, 10, 4, 4);
  });

  var texMadera = textura(32, 32, function (ctx, w, h) {
    for (var x = 0; x < w; x++) {
      var tablon = Math.floor(x / 8);
      for (var y = 0; y < h; y++) {
        var v = 0.75 + 0.25 * Math.sin(x * 2.1 + tablon * 5 + Math.sin(y * 0.4 + tablon) * 1.5);
        var r = Math.floor(61 * v), g = Math.floor(36 * v), b = Math.floor(23 * v);
        if (x % 8 === 0 || (y === (tablon * 11) % h)) { r = 11; g = 7; b = 5; }
        ctx.fillStyle = "rgb(" + r + "," + g + "," + b + ")";
        ctx.fillRect(x, y, 1, 1);
      }
    }
  }, true);

  var texPano = textura(32, 32, function (ctx, w, h) {
    for (var y = 0; y < h; y++)
      for (var x = 0; x < w; x++) {
        var n = Math.random() * 0.3 + 0.7;
        ctx.fillStyle = "rgb(" + Math.floor(64 * n) + "," + Math.floor(20 * n) + "," + Math.floor(10 * n) + ")";
        ctx.fillRect(x, y, 1, 1);
      }
  }, true);

  var texTela = textura(8, 8, function (ctx, w, h) {
    for (var y = 0; y < h; y++)
      for (var x = 0; x < w; x++) {
        var n = 0.8 + Math.random() * 0.2;
        ctx.fillStyle = "rgb(" + Math.floor(40 * n) + "," + Math.floor(28 * n) + "," + Math.floor(20 * n) + ")";
        ctx.fillRect(x, y, 1, 1);
      }
  });

  var texLiso = textura(2, 2, function (ctx) { ctx.fillStyle = "#fff"; ctx.fillRect(0, 0, 2, 2); });

  // ---- El shader PS1
  var LUZ = new THREE.Vector3(0, 2.75, -0.3);

  var vertexPS1 = [
    "uniform vec2 resolucion;",
    "uniform vec3 posLuz;",
    "uniform float potenciaLuz;",
    "uniform float emisivo;",
    "uniform vec2 repetirUv;",
    "varying vec3 vUvW;",
    "varying float vLuz;",
    "varying float vNiebla;",
    "void main() {",
    "  vec4 mundo = modelMatrix * vec4(position, 1.0);",
    "  vec3 n = normalize(mat3(modelMatrix) * normal);",
    "  vec3 l = posLuz - mundo.xyz;",
    "  float d = length(l);",
    "  float difusa = max(dot(n, l / d), 0.0) * 0.8 + 0.2;",
    // la luz cae rápido: fuera del círculo de la lámpara todo es oscuridad
    "  vLuz = max(0.03 + difusa * potenciaLuz / (1.0 + d * d * 0.55), emisivo);",
    "  vec4 vista = viewMatrix * mundo;",
    "  vNiebla = smoothstep(2.5, 7.5, -vista.z);",
    "  vec4 p = projectionMatrix * vista;",
    "  vec2 grilla = resolucion * 0.5;",
    "  p.xy = floor(p.xy / p.w * grilla + 0.5) / grilla * p.w;",   // temblor de vértices
    "  vUvW = vec3(uv * repetirUv * p.w, p.w);",                    // texturas sin perspectiva
    "  gl_Position = p;",
    "}"
  ].join("\n");

  var fragmentPS1 = [
    "uniform sampler2D mapa;",
    "uniform vec3 tinte;",
    "uniform vec3 colorLuz;",
    "uniform vec3 colorNiebla;",
    "varying vec3 vUvW;",
    "varying float vLuz;",
    "varying float vNiebla;",
    "float bayer2(vec2 a) { a = floor(a); return fract(dot(a, vec2(0.5, a.y * 0.75))); }",
    "float bayer4(vec2 a) { return bayer2(0.5 * a) * 0.25 + bayer2(a); }",
    "void main() {",
    "  vec2 uv = vUvW.xy / vUvW.z;",
    "  vec3 c = texture2D(mapa, uv).rgb * tinte * colorLuz * vLuz;",
    "  c = mix(c, colorNiebla, vNiebla);",
    // solo 16 niveles por canal: bandas marcadas suavizadas con dithering
    "  float d = bayer4(gl_FragCoord.xy) - 0.5;",
    "  c = floor(c * 15.0 + d + 0.5) / 15.0;",
    "  gl_FragColor = vec4(c, 1.0);",
    "}"
  ].join("\n");

  var materiales = [];
  function materialPS1(mapa, tinte, opciones) {
    opciones = opciones || {};
    var m = new THREE.ShaderMaterial({
      uniforms: {
        mapa: { value: mapa },
        tinte: { value: new THREE.Color(tinte === undefined ? 0xffffff : tinte) },
        resolucion: { value: new THREE.Vector2(ANCHO, ALTO) },
        posLuz: { value: LUZ },
        potenciaLuz: { value: 3.0 },
        colorLuz: { value: new THREE.Color(1.0, 0.72, 0.42) },
        colorNiebla: { value: new THREE.Color(0x050302) },
        emisivo: { value: opciones.emisivo || 0 },
        repetirUv: { value: new THREE.Vector2(opciones.repetir || 1, opciones.repetir || 1) }
      },
      vertexShader: vertexPS1,
      fragmentShader: fragmentPS1,
      side: opciones.dosCaras ? THREE.DoubleSide : THREE.FrontSide
    });
    materiales.push(m);
    return m;
  }

  // Cono de luz "semi-transparente" a la PS1: puntos sueltos con un patrón de trama
  var materialCono = new THREE.ShaderMaterial({
    uniforms: { alfa: { value: 0.16 } },
    vertexShader: "varying float vAlto; void main(){ vAlto = position.y; gl_Position = projectionMatrix * modelViewMatrix * vec4(position, 1.0); }",
    fragmentShader: [
      "uniform float alfa;",
      "varying float vAlto;",
      "float bayer2(vec2 a) { a = floor(a); return fract(dot(a, vec2(0.5, a.y * 0.75))); }",
      "float bayer4(vec2 a) { return bayer2(0.5 * a) * 0.25 + bayer2(a); }",
      "void main() {",
      "  float a = alfa * smoothstep(-0.9, 0.9, vAlto);",
      "  if (bayer4(gl_FragCoord.xy) > a) discard;",
      "  gl_FragColor = vec4(0.85, 0.5, 0.2, 1.0);",
      "}"
    ].join("\n"),
    side: THREE.DoubleSide,
    depthWrite: false
  });

  function malla(geo, mat, x, y, z, padre) {
    var m = new THREE.Mesh(geo, mat);
    m.position.set(x || 0, y || 0, z || 0);
    (padre || escena).add(m);
    return m;
  }

  // ---- El cuarto
  var piso = malla(new THREE.PlaneGeometry(20, 20), materialPS1(texMadera, 0xffffff, { repetir: 7 }));
  piso.rotation.x = -Math.PI / 2;
  malla(new THREE.PlaneGeometry(20, 8), materialPS1(texMadera, 0x888888, { repetir: 6 }), 0, 4, -5);

  // ---- Mesa redonda de pocas caras
  var CENTRO_MESA = new THREE.Vector3(0, 1.0, -0.3);
  malla(new THREE.CylinderGeometry(1.9, 1.9, 0.1, 9), materialPS1(texPano), CENTRO_MESA.x, 1.0, CENTRO_MESA.z);
  malla(new THREE.CylinderGeometry(2.05, 2.0, 0.14, 9), materialPS1(texMadera, 0xffffff, { repetir: 3 }), CENTRO_MESA.x, 0.93, CENTRO_MESA.z);
  malla(new THREE.CylinderGeometry(0.22, 0.5, 0.9, 6), materialPS1(texMadera), CENTRO_MESA.x, 0.45, CENTRO_MESA.z);

  // ---- Lámpara colgante y su cono de luz
  malla(new THREE.CylinderGeometry(0.015, 0.015, 2.0, 3), materialPS1(texLiso, 0x111111), LUZ.x, 4.0, LUZ.z);
  var pantalla = malla(new THREE.ConeGeometry(0.42, 0.34, 6, 1, true), materialPS1(texLiso, 0x5a3620, { dosCaras: true }), LUZ.x, 3.0, LUZ.z);
  var bombilla = malla(new THREE.IcosahedronGeometry(0.1, 0), materialPS1(texLiso, 0xffd28a, { emisivo: 1.8 }), LUZ.x, 2.85, LUZ.z);
  var cono = malla(new THREE.ConeGeometry(1.5, 1.8, 8, 1, true), materialCono, LUZ.x, 1.95, LUZ.z);

  // ---- El tahúr, en la penumbra al otro lado de la mesa
  var tahur = new THREE.Group();
  tahur.position.set(0, 0, -2.55);
  escena.add(tahur);

  var matAbrigo = materialPS1(texTela, 0x777777);
  var matPiel = materialPS1(texLiso, 0x4a3326);
  var matSombrero = materialPS1(texTela, 0x5a4535);

  var torso = malla(new THREE.BoxGeometry(0.95, 1.05, 0.5), matAbrigo, 0, 1.55, 0, tahur);
  torso.rotation.x = 0.18;   // encorvado sobre la mesa
  malla(new THREE.BoxGeometry(1.25, 0.22, 0.5), matAbrigo, 0, 2.0, 0.08, tahur);   // hombros

  var cabeza = new THREE.Group();
  cabeza.position.set(0, 2.32, 0.18);
  tahur.add(cabeza);
  malla(new THREE.IcosahedronGeometry(0.26, 0), matPiel, 0, 0, 0, cabeza);
  malla(new THREE.CylinderGeometry(0.58, 0.58, 0.035, 8), matSombrero, 0, 0.16, 0, cabeza);   // ala
  malla(new THREE.CylinderGeometry(0.24, 0.28, 0.34, 6), matSombrero, 0, 0.34, 0, cabeza);    // copa

  // Ojos: dos puntitos que brillan solos en la oscuridad
  var matOjo = materialPS1(texLiso, 0xffe0a0, { emisivo: 2.2 });
  var ojos = new THREE.Group();
  ojos.position.set(0, 0.02, 0.24);
  cabeza.add(ojos);
  malla(new THREE.BoxGeometry(0.06, 0.035, 0.02), matOjo, -0.09, 0, 0, ojos);
  malla(new THREE.BoxGeometry(0.06, 0.035, 0.02), matOjo, 0.09, 0, 0, ojos);

  // Brazos apoyados y manos sobre la mesa
  [-1, 1].forEach(function (lado) {
    var brazo = malla(new THREE.BoxGeometry(0.2, 0.2, 0.95), matAbrigo, lado * 0.55, 1.55, 0.55, tahur);
    brazo.rotation.x = 0.55;
    malla(new THREE.BoxGeometry(0.2, 0.08, 0.24), matPiel, lado * 0.5, 1.1, 1.08, tahur);
  });

  // Su mano de cartas, boca abajo frente a él
  var geoCarta = new THREE.PlaneGeometry(0.34, 0.5);
  var matCarta = materialPS1(texCarta, 0xffffff, { dosCaras: true });
  for (var j = 0; j < 4; j++) {
    var cartaTahur = malla(geoCarta, matCarta, -0.22 + j * 0.15, 1.065 + j * 0.003, -1.45);
    cartaTahur.rotation.set(-Math.PI / 2, 0, (j - 1.5) * 0.18);
  }

  // ---- El monto en el centro
  for (var i = 0; i < 7; i++) {
    var c = malla(geoCarta, matCarta, CENTRO_MESA.x + (Math.random() - 0.5) * 0.06, 1.06 + i * 0.01, CENTRO_MESA.z + (Math.random() - 0.5) * 0.06);
    c.rotation.set(-Math.PI / 2, 0, (Math.random() - 0.5) * 0.7);
  }

  // ---- Tu mano: cinco cartas en abanico, en primer plano
  var mano = new THREE.Group();
  escena.add(mano);
  for (var k = 0; k < 5; k++) {
    var cartaMano = malla(geoCarta, matCarta, (k - 2) * 0.09, Math.abs(k - 2) * -0.015, k * 0.004, mano);
    cartaMano.rotation.z = (2 - k) * 0.14;
  }

  // ---- Mirar con el mouse (o el dedo)
  var mirada = { x: 0, y: 0 }, objetivo = { x: 0, y: 0 };
  window.addEventListener("pointermove", function (e) {
    objetivo.x = (e.clientX / window.innerWidth) * 2 - 1;
    objetivo.y = (e.clientY / window.innerHeight) * 2 - 1;
  });

  // ---- Animación
  var reloj = new THREE.Clock();
  var proximoParpadeo = 3, proximoApagon = 7;

  function cuadro() {
    var t = reloj.getElapsedTime();

    // la lámpara tiembla y de vez en cuando casi se apaga
    var llama = 3.0 + Math.sin(t * 6.1) * 0.18 + Math.sin(t * 15.7) * 0.1;
    if (t > proximoApagon) {
      llama *= 0.15;
      if (t > proximoApagon + 0.12) proximoApagon = t + 5 + Math.random() * 8;
    }
    materiales.forEach(function (m) { m.uniforms.potenciaLuz.value = llama; });
    materialCono.uniforms.alfa.value = 0.16 * llama / 3.0;
    pantalla.rotation.z = Math.sin(t * 0.8) * 0.05;
    bombilla.position.x = LUZ.x - pantalla.rotation.z * 0.25;
    cono.rotation.z = pantalla.rotation.z;

    // el tahúr respira, ladea la cabeza y parpadea
    torso.scale.y = 1 + Math.sin(t * 1.3) * 0.012;
    cabeza.rotation.z = Math.sin(t * 0.37) * 0.08;
    cabeza.rotation.y = mirada.x * -0.25;   // te sigue con la mirada
    if (t > proximoParpadeo) {
      ojos.scale.y = 0.05;
      if (t > proximoParpadeo + 0.14) { ojos.scale.y = 1; proximoParpadeo = t + 2 + Math.random() * 4; }
    }

    // cámara: tú, sentado, respirando y mirando hacia donde está el mouse
    mirada.x += (objetivo.x - mirada.x) * 0.05;
    mirada.y += (objetivo.y - mirada.y) * 0.05;
    camara.position.set(mirada.x * 0.25, 1.78 + Math.sin(t * 1.1) * 0.015, 2.15);
    camara.lookAt(mirada.x * 1.4, 1.42 - mirada.y * 0.5, -1.2);

    // la mano sigue a la cámara, abajo a la derecha del cuadro
    mano.position.set(camara.position.x + 0.32, camara.position.y - 0.5, camara.position.z - 0.75);
    mano.rotation.set(-0.95, 0, 0.12 + Math.sin(t * 0.9) * 0.02);

    renderer.render(escena, camara);
    if (!reducirMovimiento) requestAnimationFrame(cuadro);
  }
  cuadro();
})();
