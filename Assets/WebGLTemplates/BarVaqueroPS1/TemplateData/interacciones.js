// LIAR GAME · Plantilla PS1: grano, filtro CRT, contrato y objetos.
(function () {
  "use strict";

  var reducirMovimiento = window.matchMedia && window.matchMedia("(prefers-reduced-motion: reduce)").matches;

  function guardar(clave, valor) { try { localStorage.setItem(clave, valor); } catch (e) {} }
  function leer(clave) { try { return localStorage.getItem(clave); } catch (e) { return null; } }

  // ---------- Grano de película (ruido que cambia ~12 veces por segundo)
  var grano = document.getElementById("grano");
  if (grano) {
    var g = grano.getContext("2d");
    var img = g.createImageData(grano.width, grano.height);
    var pintarGrano = function () {
      for (var i = 0; i < img.data.length; i += 4) {
        var v = Math.random() * 255;
        img.data[i] = img.data[i + 1] = img.data[i + 2] = v;
        img.data[i + 3] = 255;
      }
      g.putImageData(img, 0, 0);
    };
    pintarGrano();
    if (!reducirMovimiento) setInterval(pintarGrano, 80);
  }

  // ---------- Filtro CRT on/off (se recuerda en este navegador)
  var botonCrt = document.getElementById("crt-toggle");
  function aplicarCrt(encendido) {
    document.body.classList.toggle("crt-on", encendido);
    botonCrt.textContent = "CRT: " + (encendido ? "ON" : "OFF");
  }
  if (leer("liar-crt") === "off") aplicarCrt(false);
  botonCrt.addEventListener("click", function () {
    var encendido = !document.body.classList.contains("crt-on");
    aplicarCrt(encendido);
    guardar("liar-crt", encendido ? "on" : "off");
  });

  // ---------- Contrato: firmar con tu nombre (máx. 6 letras)
  var form = document.getElementById("firma");
  var campo = document.getElementById("firma-nombre");
  var sello = document.getElementById("firmado");
  var saludo = document.getElementById("saludo");

  function limpiarNombre(texto) {
    return texto.toUpperCase().replace(/[^A-ZÑ0-9]/g, "").slice(0, 6);
  }

  function mostrarFirmado(nombre) {
    campo.value = nombre;
    sello.hidden = false;
    saludo.textContent = "siéntate, " + nombre + ". nadie dice la verdad aquí.";
  }

  campo.addEventListener("input", function () { campo.value = limpiarNombre(campo.value); });

  form.addEventListener("submit", function (e) {
    e.preventDefault();
    var nombre = limpiarNombre(campo.value);
    if (!nombre) {
      campo.placeholder = "??????";
      campo.focus();
      return;
    }
    mostrarFirmado(nombre);
    guardar("liar-nombre", nombre);
    setTimeout(function () {
      document.getElementById("jugar").scrollIntoView({ behavior: reducirMovimiento ? "auto" : "smooth" });
    }, 900);
  });

  var nombreGuardado = leer("liar-nombre");
  if (nombreGuardado) mostrarFirmado(limpiarNombre(nombreGuardado));

  // ---------- Poderes como objetos: descripción al señalar uno
  var objetos = document.querySelectorAll(".objeto");
  var nombreObjeto = document.getElementById("objeto-nombre");
  var textoObjeto = document.getElementById("objeto-texto");

  function describir(obj) {
    objetos.forEach(function (o) { o.classList.toggle("activo", o === obj); });
    nombreObjeto.textContent = obj.dataset.nombre;
    textoObjeto.textContent = obj.dataset.texto;
  }

  objetos.forEach(function (obj) {
    obj.addEventListener("mouseenter", function () { describir(obj); });
    obj.addEventListener("focus", function () { describir(obj); });
    obj.addEventListener("click", function () { describir(obj); });
  });
})();
