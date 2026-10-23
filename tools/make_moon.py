"""Generates Assets/Art/Environment/Moon.png (lit lunar disc + soft halo) and SkyGradient.png.

Run from the repo root:  python tools/make_moon.py
Pure numpy + stdlib; deterministic (fixed seed) so re-running reproduces the committed art.
"""
import struct
import zlib

import numpy as np

S = 512          # sprite size (px)
R = 150.0        # disc radius (px); the rest of the sprite is halo
OUT = "Assets/Art/Environment/"
rng = np.random.default_rng(7)


def write_png(path, rgba):
    h, w, _ = rgba.shape
    raw = b"".join(b"\x00" + rgba[y].tobytes() for y in range(h))
    chunk = lambda t, d: struct.pack(">I", len(d)) + t + d + struct.pack(">I", zlib.crc32(t + d))
    with open(path, "wb") as f:
        f.write(b"\x89PNG\r\n\x1a\n" + chunk(b"IHDR", struct.pack(">IIBBBBB", w, h, 8, 6, 0, 0, 0))
                + chunk(b"IDAT", zlib.compress(raw, 9)) + chunk(b"IEND", b""))


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def value_noise(u, v, freq, seed):
    g = np.random.default_rng(seed).random((freq + 1, freq + 1))
    x, y = u * freq, v * freq
    x0, y0 = np.clip(np.floor(x).astype(int), 0, freq - 1), np.clip(np.floor(y).astype(int), 0, freq - 1)
    fx, fy = smoothstep(0, 1, x - x0), smoothstep(0, 1, y - y0)
    a = g[y0, x0] * (1 - fx) + g[y0, x0 + 1] * fx
    b = g[y0 + 1, x0] * (1 - fx) + g[y0 + 1, x0 + 1] * fx
    return a * (1 - fy) + b * fy


def fbm(u, v, base, octaves, seed):
    total, amp, norm = 0.0, 1.0, 0.0
    for o in range(octaves):
        total = total + amp * value_noise(u, v, base * 2 ** o, seed + o)
        norm += amp
        amp *= 0.5
    return total / norm


# Pixel grid, y up, centred on the sprite.
yy, xx = np.mgrid[S - 1:-1:-1, 0:S].astype(float)
dx, dy = (xx - S / 2 + 0.5) / R, (yy - S / 2 + 0.5) / R
r = np.sqrt(dx * dx + dy * dy)
inside = r < 1.0
nz = np.sqrt(np.clip(1 - r * r, 0, 1))
lon = np.arctan2(dx, nz)                        # surface coords, so features foreshorten toward the limb
lat = np.arcsin(np.clip(dy, -1, 1))
u, v = (lon / np.pi + 0.5), (lat / np.pi + 0.5)

# Albedo: bright highlands with dark, irregular maria (domain-warped so the edges meander) and fine grain.
wu, wv = u + 0.10 * fbm(u, v, 5, 3, 21), v + 0.10 * fbm(u, v, 5, 3, 31)
maria = smoothstep(0.55, 0.62, fbm(wu, wv, 4, 5, 11))
albedo = 0.92 - 0.30 * maria + 0.09 * (fbm(u, v, 22, 4, 40) - 0.5)

# Craters, sized on a power law so most are small: dark floor, soft bright rim, a shallow bowl for relief,
# and faint bright ejecta around the larger ones.
height = 0.04 * fbm(u, v, 24, 3, 70)
for _ in range(30):
    cu, cv = rng.uniform(0.15, 0.85), rng.uniform(0.15, 0.85)
    cr = 0.008 + 0.05 * rng.random() ** 4
    d = np.sqrt(((u - cu) * np.cos((cv - 0.5) * np.pi)) ** 2 + (v - cv) ** 2) / cr
    height += cr * 3 * (-(1 - d * d) * (d < 1) + 0.4 * np.exp(-((d - 1.0) / 0.3) ** 2))
    albedo += -0.05 * (d < 0.9) * smoothstep(0.9, 0.6, d) + 0.05 * np.exp(-((d - 1.0) / 0.3) ** 2)
    albedo += 0.07 * np.exp(-(d / 2.5) ** 2) * (cr > 0.025)
gy, gx = np.gradient(height) * smoothstep(0.995, 0.95, r)   # surface coords clamp past the limb; no bump there
bump = 4.0
n = np.stack([dx - bump * gx, dy + bump * gy, nz], -1)  # rows run downward, hence +gy
n /= np.linalg.norm(n, axis=-1, keepdims=True)

# Light from the upper left: the lower-right limb rolls off into a soft blue-grey shadow (never transparent,
# so the disc stays round) which gives the sphere volume without a hard terminator.
L = np.array([-0.62, 0.45, 0.64])
L /= np.linalg.norm(L)
lambert = np.clip((n * L).sum(-1), 0, 1)
shade = (0.22 + 0.78 * lambert) * albedo

shadow = np.array([0.33, 0.37, 0.46])   # desaturated blue-grey
mid = np.array([0.66, 0.68, 0.71])      # cool silver
light = np.array([0.91, 0.89, 0.82])    # muted ivory, never pure white
t = np.clip(shade, 0, 1)[..., None]
moon_rgb = np.where(t < 0.55, shadow + (mid - shadow) * (t / 0.55), mid + (light - mid) * ((t - 0.55) / 0.45))
moon_rgb = np.clip(moon_rgb, 0, 0.93)

# Soft anti-aliased edge, then a restrained two-part halo that fades out before the sprite border.
disc_a = smoothstep(1.0 + 1.2 / R, 1.0 - 1.2 / R, r)
# Halo centred slightly toward the light: fuller on the lit limb, thinner beside the shadowed one.
r_halo = np.sqrt((dx + 0.05) ** 2 + (dy - 0.04) ** 2)
outer = np.clip(r_halo - 1.0, 0, None)
halo_a = (0.17 * np.exp(-outer / 0.09) + 0.055 * np.exp(-outer / 0.34)) * smoothstep(1.66, 1.35, r)
halo_rgb = np.array([0.80, 0.84, 0.90])
# Pad the limb colour a few texels outward before fading to the halo colour: otherwise mip/bilinear averaging of
# the dark shadowed limb with the bright (low-alpha) halo texels draws a light fringe around the disc.
rows, cols = np.mgrid[0:S, 0:S].astype(float)
k = np.minimum(1.0, 0.985 / np.maximum(r, 1e-6))
c0 = S / 2 - 0.5
limb_rgb = moon_rgb[np.round(c0 + (rows - c0) * k).astype(int), np.round(c0 + (cols - c0) * k).astype(int)]
halo_rgb = limb_rgb + (halo_rgb - limb_rgb) * smoothstep(1.0, 1.03, r)[..., None]

a = disc_a + halo_a * (1 - disc_a)
# Straight-alpha colour of "disc over halo"; weighting the halo by its own alpha avoids a bright fringe at the limb.
w_halo = (halo_a * (1 - disc_a))[..., None]
rgb = (np.where(inside[..., None], moon_rgb, halo_rgb) * disc_a[..., None] + halo_rgb * w_halo) / np.maximum(a[..., None], 1e-6)
rgb = np.where(a[..., None] > 1e-6, rgb, halo_rgb)   # fully transparent texels keep the halo colour so filtering can't bleed dark
img = np.dstack([rgb, a])
write_png(OUT + "Moon.png", np.round(np.clip(img, 0, 1) * 255).astype(np.uint8))

# Sky: the existing camera blue at the horizon (where the gameplay sits), deepening to midnight navy overhead.
H = 256
vt = smoothstep(0.30, 1.0, np.linspace(0, 1, H))[::-1, None]   # row 0 = top
horizon = np.array([49, 77, 121]) / 255.0
zenith = np.array([16, 24, 48]) / 255.0
sky = horizon + (zenith - horizon) * vt
sky = np.repeat(np.concatenate([sky, np.ones((H, 1))], 1)[:, None, :], 4, axis=1)
write_png(OUT + "SkyGradient.png", np.round(sky * 255).astype(np.uint8))
print("wrote Moon.png and SkyGradient.png")
