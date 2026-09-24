#!/usr/bin/env python3
"""Test-only helper: installs Open Sans under the family names WPF asks for
(Segoe UI, Tahoma, Arial…) into a Wine prefix, so previews rendered on Linux
lay out text close to Windows 11. Never shipped with the app.

Open Sans has no arrows (↑ ↓ → ↔), which Segoe UI does have, so those glyphs
are borrowed from DejaVu Sans; otherwise previews would show empty boxes.

Usage: python3 tools/preview/wine_fonts.py <wine-prefix>
"""
import os
import sys

from fontTools.pens.recordingPen import DecomposingRecordingPen
from fontTools.pens.transformPen import TransformPen
from fontTools.pens.ttGlyphPen import TTGlyphPen
from fontTools.ttLib import TTFont

SRC = "/usr/share/fonts/truetype/open-sans/"
FAMILIES = ["Segoe UI", "Tahoma", "Segoe UI Variable Text", "Segoe UI Variable Display", "Arial", "Microsoft Sans Serif"]
WEIGHTS = {"Regular": "OpenSans-Regular.ttf", "Semibold": "OpenSans-Semibold.ttf", "Bold": "OpenSans-Bold.ttf"}

DONOR = "/usr/share/fonts/truetype/dejavu/"
DONORS = {"Regular": "DejaVuSans.ttf", "Semibold": "DejaVuSans.ttf", "Bold": "DejaVuSans-Bold.ttf"}
BORROWED = [(0x2190, 0x21FF), (0x202F, 0x202F)]  # arrows, narrow no-break space


def borrow_glyphs(font, donor):
    """Copies glyphs the font lacks from the donor, decomposed and scaled to the font's em."""
    scale = font["head"].unitsPerEm / donor["head"].unitsPerEm
    donor_cmap, donor_set = donor.getBestCmap(), donor.getGlyphSet()
    cmap = font.getBestCmap()
    glyf, hmtx = font["glyf"], font["hmtx"]
    order = list(font.getGlyphOrder())
    added = {}
    for lo, hi in BORROWED:
        for cp in range(lo, hi + 1):
            if cp in cmap or cp not in donor_cmap:
                continue
            source, name = donor_cmap[cp], f"uni{cp:04X}"
            recording = DecomposingRecordingPen(donor_set)
            donor_set[source].draw(recording)
            pen = TTGlyphPen(None)
            recording.replay(TransformPen(pen, (scale, 0, 0, scale, 0, 0)))
            glyph = pen.glyph()
            glyf.glyphs[name] = glyph
            glyph.recalcBounds(glyf)
            hmtx.metrics[name] = (round(donor["hmtx"][source][0] * scale), getattr(glyph, "xMin", 0))
            order.append(name)
            added[cp] = name
    font.setGlyphOrder(order)
    glyf.glyphOrder = order
    for table in font["cmap"].tables:
        if table.isUnicode():
            table.cmap.update(added)
    for tag in ("hdmx", "LTSH", "VDMX"):  # per-glyph caches that would no longer match
        if tag in font:
            del font[tag]


def main():
    prefix = sys.argv[1]
    fonts = os.path.join(prefix, "drive_c", "windows", "Fonts")
    os.makedirs(fonts, exist_ok=True)
    reg = ["Windows Registry Editor Version 5.00", "",
           r"[HKEY_LOCAL_MACHINE\Software\Microsoft\Windows NT\CurrentVersion\Fonts]"]
    for family in FAMILIES:
        for style, file in WEIGHTS.items():
            font = TTFont(os.path.join(SRC, file))
            borrow_glyphs(font, TTFont(os.path.join(DONOR, DONORS[style])))
            ps = (family + "-" + style).replace(" ", "")
            for rec in font["name"].names:
                if rec.nameID == 1:
                    rec.string = family + (" Semibold" if style == "Semibold" else "")
                elif rec.nameID == 2 and style == "Semibold":
                    rec.string = "Regular"
                elif rec.nameID == 16:
                    rec.string = family
                elif rec.nameID == 17 and style == "Semibold":
                    rec.string = "Semibold"
                elif rec.nameID == 4:
                    rec.string = f"{family} {style}"
                elif rec.nameID in (3, 6):
                    rec.string = ps
            if style == "Semibold":
                font["OS/2"].usWeightClass = 600
            font.save(os.path.join(fonts, ps + ".ttf"))
            reg.append(f'"{family} {style} (TrueType)"="{ps}.ttf"')
    with open(os.path.join(prefix, "grip-fonts.reg"), "w", encoding="utf-8") as f:
        f.write("\r\n".join(reg) + "\r\n")


if __name__ == "__main__":
    main()
