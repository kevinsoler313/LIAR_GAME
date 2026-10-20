"""Lleva la baraja (línea negra sobre blanco) a la paleta de bar vaquero del juego.

Uso:  python3 Herramientas/estilizar_cartas.py <carpeta con los PNG originales> Assets/Cartas/Sprites
Requiere Pillow y numpy. Genera también carta-reverso.png.
"""
import sys, os
import numpy as np
from PIL import Image, ImageDraw, ImageFilter

src, dst = sys.argv[1], sys.argv[2]
os.makedirs(dst, exist_ok=True)
rng = np.random.default_rng(7)

def hx(h):
    return np.array([int(h[i:i + 2], 16) for i in (0, 2, 4)], dtype=np.float32)

negro, cafe, cafeOscuro = hx("0b0705"), hx("3d2417"), hx("241510")
mostaza, mostazaTenue = hx("c9a24a"), hx("8f733a")
naranja, rojoOxido = hx("d9661f"), hx("6a1e08")
papel, tinta, mancha = hx("d9c39a"), hx("2a1a10"), hx("59300f")

W, H = 750, 1050

def ruido(escala, sigma):
    n = Image.fromarray((rng.random((H // escala, W // escala)) * 255).astype(np.uint8))
    n = n.resize((W, H), Image.BICUBIC).filter(ImageFilter.GaussianBlur(sigma))
    return np.asarray(n, dtype=np.float32) / 255.0

def lerp(a, b, t):
    return a + (b - a) * t[..., None]

# Papel envejecido: grano + manchas + viñeta hacia los bordes (como el póster "Se busca")
yy, xx = np.mgrid[0:H, 0:W].astype(np.float32)
d = np.sqrt(((xx / W) - 0.5) ** 2 + ((yy / H) - 0.5) ** 2) / 0.7071
vineta = np.clip((d - 0.35) / 0.65, 0, 1) ** 1.6
manchas = np.clip(ruido(14, 3) * 1.8 - 0.95, 0, 1)
grano = ruido(1, 0.6) - 0.5
textura_papel = lerp(np.broadcast_to(papel, (H, W, 3)), mancha, np.clip(vineta * 0.8 + manchas * 0.12, 0, 1))
textura_papel = textura_papel * (1 + grano[..., None] * 0.08)

# Madera para la franja del marco (vetas verticales)
veta = ruido(3, 1.2)
fibra = np.asarray(Image.fromarray((rng.random((H // 40, W)) * 255).astype(np.uint8)).resize((W, H), Image.BICUBIC), dtype=np.float32) / 255
madera = (0.75 + 0.35 * veta[..., None]) * (0.92 + 0.12 * fibra[..., None])

ESTILOS = {
    # tipo: (color de franja, color de tinta, color del hexágono central)
    "numero": (cafe, tinta, naranja),
    "poder": (rojoOxido * 0.85, rojoOxido, rojoOxido),
    "comodin": (mostazaTenue, tinta, naranja),
}

def tipo_de(nombre):
    if any(p in nombre for p in ("pass", "return", "break")): return "poder"
    if "infinito" in nombre: return "comodin"
    return "numero"

def masks(img):
    a = np.asarray(img, dtype=np.float32)
    alpha = a[..., 3] / 255
    lum = a[..., :3].mean(axis=2) / 255
    tinta_amt = (1 - lum) * alpha                                   # 0 = blanco, 1 = negro (antialias)

    # Franja del marco: blanco conectado al punto (20, 525) entre los dos bordes
    blanco = Image.fromarray(((lum > 0.5) & (alpha > 0.5)).astype(np.uint8) * 255).copy()   # copy: floodfill no escribe sobre el buffer de numpy
    ImageDraw.floodfill(blanco, (20, 525), 128)
    franja = np.array(blanco) == 128
    franja_suave = np.asarray(Image.fromarray(franja.astype(np.uint8) * 255).filter(ImageFilter.MaxFilter(3))) > 0

    # Línea exterior = lo opaco pegado a la transparencia; línea interior = tinta junto a la franja
    transp = Image.fromarray((alpha < 0.5).astype(np.uint8) * 255).filter(ImageFilter.MaxFilter(13))
    exterior = (np.asarray(transp) > 0) & (alpha > 0)
    cerca_franja = np.asarray(Image.fromarray(franja.astype(np.uint8) * 255).filter(ImageFilter.MaxFilter(11))) > 0
    interior = cerca_franja & ~franja & ~exterior & (tinta_amt > 0.05)
    return alpha, tinta_amt, franja_suave, exterior, interior

def estilizar(nombre):
    img = Image.open(os.path.join(src, nombre)).convert("RGBA")
    alpha, ink, franja, exterior, interior = masks(img)
    col_franja, col_tinta, col_hex = ESTILOS[tipo_de(nombre)]

    out = textura_papel.copy()

    # Tinta: los números/símbolos en café oscuro; el hexágono central (si lo hay) en su color
    color_tinta = np.broadcast_to(col_tinta, (H, W, 3)).copy()
    caja_hex = (yy > 410) & (yy < 640) & (xx > 270) & (xx < 480)
    color_tinta[caja_hex] = col_hex
    out = lerp(out, color_tinta, ink * ~(franja | exterior | interior))

    # Marco: franja de madera teñida, filete mostaza adentro y borde negro afuera
    out[franja] = (col_franja * madera)[franja]
    out[interior] = lerp(out, np.broadcast_to(mostaza, (H, W, 3)), ink)[interior]
    out[exterior] = lerp(out, np.broadcast_to(negro, (H, W, 3)), np.clip(ink * 1.2, 0, 1))[exterior]

    rgba = np.dstack([np.clip(out, 0, 255), alpha * 255]).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(os.path.join(dst, nombre))
    return alpha

def reverso(alpha):
    """Reverso al estilo de las cartas del menú: rayas diagonales café, marco negro, hexágono naranja con aro mostaza."""
    out = np.where((((xx + yy) // 42) % 2 == 0)[..., None], cafe, cafeOscuro).astype(np.float32)
    out *= (0.85 + 0.3 * veta[..., None])

    def hexagono(cx, cy, r):
        pts = [(cx + r * np.cos(np.radians(90 + 60 * i)), cy + r * np.sin(np.radians(90 + 60 * i))) for i in range(6)]
        m = Image.new("L", (W, H), 0)
        ImageDraw.Draw(m).polygon(pts, fill=255)
        return np.asarray(m.filter(ImageFilter.GaussianBlur(0.8)), dtype=np.float32) / 255

    cx, cy = W / 2, H / 2
    for r, c in ((150, negro), (140, mostaza), (128, negro), (116, mostazaTenue), (104, negro), (78, naranja)):
        out = lerp(out, np.broadcast_to(c, (H, W, 3)), hexagono(cx, cy, r))

    # Marco: el mismo contorno de la carta (alpha) erosionado para hacer bordes concéntricos
    a = Image.fromarray((alpha * 255).astype(np.uint8))
    def erosion(px):
        return np.asarray(a.filter(ImageFilter.MinFilter(px)), dtype=np.float32) / 255 if px > 1 else alpha
    out = lerp(np.broadcast_to(mostazaTenue, (H, W, 3)).astype(np.float32), out, erosion(23))   # borde dorado exterior
    marco_negro = erosion(31) - erosion(51)
    out = lerp(out, np.broadcast_to(negro, (H, W, 3)), np.clip(marco_negro, 0, 1))
    out = lerp(out, np.broadcast_to(negro, (H, W, 3)), np.clip(alpha - erosion(7), 0, 1))       # filo negro

    rgba = np.dstack([np.clip(out, 0, 255), alpha * 255]).astype(np.uint8)
    Image.fromarray(rgba, "RGBA").save(os.path.join(dst, "carta-reverso.png"))

alpha = None
for f in sorted(os.listdir(src)):
    if f.endswith(".png"):
        alpha = estilizar(f)
        print("ok", f, tipo_de(f))
reverso(alpha)
print("ok carta-reverso.png")
