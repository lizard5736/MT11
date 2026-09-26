#!/usr/bin/env python3
"""Builds the DOOM theme's pixel-art Grip mark: a chunky, hard-edged version of the same
ring+crossbar+dot silhouette GripMark.cs draws procedurally for every other theme (see that
file's Draw() method — this mirrors its geometry: a ring swept 290 degrees clockwise from
3 o'clock, a crossbar from the center to the ring, a dot in the opening).

Drawn on a small integer grid with nearest-neighbor logic (no anti-aliasing, no PIL shape
primitives that would soften edges) so the output is genuinely blocky pixel art, not a
downsampled smooth icon. One 24x24 source serves every usage — the tray icon at close to its
native size, and the larger panel header/settings/onboarding/about mark scaled up further at
render time with nearest-neighbor filtering, so the same chunky-pixel look carries through
everywhere instead of a smoother, bigger icon appearing next to a blockier small one.

Usage: python3 tools/icons/build_doom_mark.py
"""
import math
import os

from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT_DIR = os.path.join(ROOT, "src", "Grip", "Assets", "Doom")

RING_COLOR = (255, 90, 31, 255)  # Grip.Color.Accent
DOT_COLOR = (229, 28, 28, 255)  # Grip.Color.Rec

# Ring sweeps clockwise from 0 degrees (3 o'clock) to 290 degrees, leaving a 70-degree opening
# in the upper right where the dot sits — same angles as GripMark.Draw().
RING_START_DEG = 0
RING_SWEEP_DEG = 290


def draw_mark(size: int) -> Image.Image:
    img = Image.new("RGBA", (size, size), (0, 0, 0, 0))
    px = img.load()
    cx = cy = (size - 1) / 2
    radius = size * 0.335
    thickness = max(2, round(size * 0.20))
    half_thick = thickness / 2

    def angle_in_sweep(deg: float) -> bool:
        d = deg % 360
        return d <= RING_SWEEP_DEG

    for y in range(size):
        for x in range(size):
            dx, dy = x - cx, y - cy
            dist = math.hypot(dx, dy)
            deg = math.degrees(math.atan2(dy, dx)) % 360

            on_ring = abs(dist - radius) <= half_thick and angle_in_sweep(deg)

            on_bar = abs(dy) <= half_thick and 0 <= dx <= radius + half_thick

            if on_ring or on_bar:
                px[x, y] = RING_COLOR

    dot_deg = math.radians(325)
    dot_x = cx + radius * math.cos(dot_deg)
    dot_y = cy + radius * math.sin(dot_deg)
    dot_r = thickness * 0.62
    for y in range(size):
        for x in range(size):
            if math.hypot(x - dot_x, y - dot_y) <= dot_r:
                px[x, y] = DOT_COLOR

    return img


def main():
    os.makedirs(OUT_DIR, exist_ok=True)
    img = draw_mark(24)
    path = os.path.join(OUT_DIR, "mark-24.png")
    img.save(path)
    print(f"wrote {path} (24x24)")


if __name__ == "__main__":
    main()
