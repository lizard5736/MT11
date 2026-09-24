#!/usr/bin/env python3
"""Draws the Grip app icon into src/Grip/Assets and docs/assets.

Usage: python3 tools/icons/build_app_icon.py   (needs Pillow)

The mark is an amber "G" (a 290° ring plus a crossbar) with a red tally dot
in its opening, like the REC light on a camera. The app draws the same
geometry at runtime for the tray icon and the panel header (see GripMark.cs).
"""
import math
import os

from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
ASSETS = os.path.join(ROOT, "src", "Grip", "Assets")
DOCS = os.path.join(ROOT, "docs", "assets")

AMBER = (255, 176, 32, 255)
RED = (255, 69, 58, 255)
TOP = (40, 40, 40)
BOTTOM = (14, 14, 14)


def mark(draw, size, cx, cy, radius, stroke, color):
    outer = radius + stroke / 2
    # PIL measures degrees clockwise from 3 o'clock: 0 → 290 leaves the upper-right gap.
    draw.arc([cx - outer, cy - outer, cx + outer, cy + outer], start=0, end=290, fill=color, width=stroke)
    half = stroke / 2
    draw.rectangle([cx + stroke * 0.25, cy - half, cx + radius + half, cy + half], fill=color)
    # The tally dot sits on the ring's centerline in the middle of the gap.
    angle = math.radians(325)
    dot = stroke * 0.62
    px, py = cx + radius * math.cos(angle), cy + radius * math.sin(angle)
    draw.ellipse([px - dot, py - dot, px + dot, py + dot], fill=RED)


def tile(size):
    scale = 8
    big = size * scale
    img = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    grad = Image.new("RGBA", (big, big))
    gd = ImageDraw.Draw(grad)
    for y in range(big):
        t = y / (big - 1)
        c = tuple(int(TOP[i] + (BOTTOM[i] - TOP[i]) * t) for i in range(3)) + (255,)
        gd.line([(0, y), (big, y)], fill=c)
    mask = Image.new("L", (big, big), 0)
    corner = int(big * 0.22)
    inset = int(big * (0.03 if size >= 48 else 0.0))
    ImageDraw.Draw(mask).rounded_rectangle([inset, inset, big - 1 - inset, big - 1 - inset], radius=corner, fill=255)
    img.paste(grad, (0, 0), mask)

    d = ImageDraw.Draw(img)
    # A faint lit rim along the top edge.
    rim = Image.new("RGBA", (big, big), (0, 0, 0, 0))
    ImageDraw.Draw(rim).rounded_rectangle([inset, inset, big - 1 - inset, big - 1 - inset], radius=corner,
                                          outline=(255, 255, 255, 34), width=max(2, big // 128))
    fade = Image.linear_gradient("L").resize((big, big)).point(lambda v: 255 - v)
    img.alpha_composite(Image.composite(rim, Image.new("RGBA", (big, big), (0, 0, 0, 0)), fade))

    radius = big * 0.24
    stroke = int(big * 0.105)
    mark(d, big, big / 2, big / 2, radius, stroke, AMBER)
    return img.resize((size, size), Image.LANCZOS)


def main():
    os.makedirs(ASSETS, exist_ok=True)
    os.makedirs(DOCS, exist_ok=True)
    sizes = [16, 20, 24, 32, 40, 48, 64, 96, 128, 256]
    frames = {s: tile(s) for s in sizes}
    ico = os.path.join(ASSETS, "grip.ico")
    frames[256].save(ico, sizes=[(s, s) for s in sizes], append_images=[frames[s] for s in sizes if s != 256])
    frames[256].save(os.path.join(DOCS, "grip-icon.png"))
    tile(512).save(os.path.join(DOCS, "grip-icon-512.png"))
    print("wrote", os.path.relpath(ico, ROOT))


if __name__ == "__main__":
    main()
