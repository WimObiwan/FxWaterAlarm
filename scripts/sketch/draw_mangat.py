#!/usr/bin/env python3
"""Drawings for Docs/Aanpassingen/mangat-volume-compensatie.md.

    python3 scripts/sketch/draw_mangat.py
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from sketchlib import P, Sketch  # noqa: E402

OUT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                    "..", "..", "Docs", "_Tekeningen"))

# a well with a narrow manhole neck, shared by three drawings
BL, BR = 200, 560          # body, left and right
BT, BB = 160, 390          # body, top and bottom
NL, NR = 330, 430          # neck, left and right
NT = 74                    # neck, top


def out(name):
    return os.path.join(OUT, name)


def _put(s):
    """Outline of the well: wide body, narrow neck on top."""
    s.polyline([P(NL, NT), P(NR, NT), P(NR, BT), P(BR, BT), P(BR, BB),
                P(BL, BB), P(BL, BT), P(NL, BT)], close=True, width=2.0)


def _water(s, top, left=BL, right=BR, bottom=BB, wave=True):
    s.polyline([P(left, top), P(right, top), P(right, bottom), P(left, bottom)],
               close=True, width=1.0, opacity=0.0, passes=1, fill=s.WATER,
               fill_opacity=0.16, hatch=18, hatch_spacing=11,
               hatch_color=s.WATER, hatch_opacity=0.45)
    if wave:
        s.wave(left, right, top, amplitude=3.0, wavelength=50, width=2.0)


# ------------------------------------------------------ below the 100 % mark
def onder_100():
    s = Sketch(800, 470, seed=201, title="Waterstand onder 100 %",
               desc="Het water staat in de brede hoofdput; het volume is "
                    "gewoon niveau maal inhoud.")
    s.title_text("Waterstand onder 100 %", y=34)

    _put(s)
    _water(s, 206)
    s.label(120, 110, "mangat", to=P(NL + 14, NT + 30), anchor="end",
            weight="600")
    s.text(120, 130, "smal, bv. 0,50 m²", size=12.5, color=s.INK_SOFT,
           anchor="end")
    s.label(680, 250, "hoofdput", to=P(BR - 30, 300), weight="600")
    s.text(680, 270, "breed, bv. 4,50 m²", size=12.5, color=s.INK_SOFT)
    s.label(120, 300, "waterniveau", to=P(BL + 40, 208), anchor="end",
            weight="600", color=s.BRAND)
    s.text(120, 320, "bv. 80 %", size=12.5, color=s.INK_SOFT, anchor="end")

    s.text(400, 434, "Volume  =  niveau × inhoud van de put", size=15,
           anchor="middle", weight="700", color=s.BRAND)
    print(s.save(out("Mangat_Onder_100.svg")))


# ------------------------------------------------------ above the 100 % mark
def boven_100():
    s = Sketch(800, 480, seed=202, title="Waterstand boven 100 %",
               desc="Boven de 100 %-markering staat het water in het smalle "
                    "mangat; per millimeter stijging komt er minder volume bij.")
    s.title_text("Waterstand boven 100 % — overloop in het mangat", y=34)

    _put(s)
    _water(s, BT, wave=False)
    _water(s, 118, left=NL, right=NR, bottom=BT, wave=True)
    s.line(BL, BT, BR, BT, stroke=s.BRAND, width=2.0, dash="9 6")

    s.label(180, 150, "water in het mangat", to=P(NL + 16, 150), anchor="end",
            size=13, weight="600", color=s.BRAND)
    s.label(620, 172, "100 %", to=P(BR - 40, BT), weight="700", color=s.BRAND)
    s.text(620, 192, "overgang put → mangat", size=12.5, color=s.INK_SOFT)
    s.text(620, 300, "de put is vol", size=13, color=s.INK_SOFT)

    s.text(400, 426, "Volume  =  inhoud van de put", size=15, anchor="middle",
           weight="700", color=s.BRAND)
    s.text(400, 450, "+  (overloophoogte × mangat­oppervlakte)", size=15,
           anchor="middle", weight="700", color=s.BRAND)
    print(s.save(out("Mangat_Boven_100.svg")))


# ------------------------------------------------------- measuring the shape
def mangat_vorm():
    s = Sketch(820, 330, seed=203, title="Oppervlakte van een rond of "
                                         "vierkant mangat",
               desc="Rond mangat van 80 cm doorsnede is 0,50 m²; vierkant "
                    "mangat van 70 bij 70 cm is 0,49 m².")
    s.title_text("De binnenmaat van het mangat opmeten", y=34)

    # round
    cx, cy, r = 220, 176, 86
    s.circle(cx, cy, r, width=2.0, fill=s.BRAND, fill_opacity=0.08)
    s.dimension(cx - r, cy, cx + r, cy, label="⌀ 80 cm", stroke=s.BRAND,
                offset=-14)
    s.text(cx, cy - r - 22, "rond mangat", size=14.5, anchor="middle",
           weight="700")
    s.text(cx, cy + r + 34, "π × 0,40² = 0,50 m²", size=14, anchor="middle",
           weight="600", color=s.BRAND)

    # square
    x, y, side = 520, 90, 172
    s.rect(x, y, side, side, width=2.0, fill=s.BRAND, fill_opacity=0.08)
    s.dimension(x, y + side / 2, x + side, y + side / 2, label="70 cm",
                stroke=s.BRAND, offset=-14)
    s.dimension(x + side + 22, y, x + side + 22, y + side, label="70 cm",
                stroke=s.BRAND, offset=8)
    s.text(x + side / 2, y - 22, "vierkant mangat", size=14.5, anchor="middle",
           weight="700")
    s.text(x + side / 2, y + side + 34, "0,70 × 0,70 = 0,49 m²", size=14,
           anchor="middle", weight="600", color=s.BRAND)

    s.caption("Meet de binnenmaat van de opening bovenaan de put.  "
              "Een schatting volstaat.")
    print(s.save(out("Mangat_Vorm.svg")))


# ------------------------------------------------------------ settings panel
def instellingen():
    s = Sketch(800, 400, seed=204, title="Het veld Mangat oppervlakte",
               desc="In de sensorinstellingen staat het veld Mangat "
                    "oppervlakte, in te vullen in vierkante meter.")
    s.title_text("Sensor­instellingen op het platform", y=34)

    x, y, w, h = 60, 62, 480, 280
    s.rect(x, y, w, h, width=2.0, fill=s.PAPER)
    s.line(x, y + 44, x + w, y + 44, width=1.4, passes=1)
    s.text(x + 20, y + 30, "Instellingen", size=15, weight="700")

    # the field names are the ones the platform actually shows
    velden = [
        (["Afstand sensor tot water", "wanneer leeg (0%)"], "3000", "mm", False),
        (["Afstand sensor tot water", "wanneer vol (100%)"], "800", "mm", False),
        (["Capaciteit"], "10000", "liter", False),
        (["Mangat oppervlakte"], "0,50", "m²", True),
    ]
    ry = y + 72
    for namen, waarde, eenheid, highlight in velden:
        kleur = s.ACCENT if highlight else s.INK
        top = ry + 5 - 9 * (len(namen) - 1)
        for i, naam in enumerate(namen):
            s.text(x + 20, top + i * 18, naam, size=12.5, color=kleur,
                   weight="600" if highlight else "500")
        s.rect(x + 250, ry - 14, 120, 28, stroke=kleur,
               width=1.8 if highlight else 1.3, fill=kleur if highlight else None,
               fill_opacity=0.12)
        s.text(x + 262, ry + 5, waarde, size=13, color=kleur)
        s.text(x + 378, ry + 5, eenheid, size=12, color=s.INK_SOFT)
        if highlight:
            s.arrow(x + w + 106, ry, x + w + 14, ry, stroke=s.ACCENT, width=1.6)
            s.text(x + w + 114, ry - 6, "in m²,", size=12.5, color=s.ACCENT,
                   weight="600")
            s.text(x + w + 114, ry + 12, "een schatting", size=12.5,
                   color=s.ACCENT, weight="600")
        ry += 46

    s.rect(x + 250, y + h - 48, 110, 32, width=1.6, fill=s.BRAND,
           fill_opacity=0.12)
    s.text(x + 305, y + h - 27, "Opslaan", size=13.5, anchor="middle",
           weight="600")

    s.caption("Laat het veld leeg als je put geen mangat heeft — dan werkt "
              "alles zoals voorheen.")
    print(s.save(out("Mangat_Instellingen.svg")))


# ---------------------------------------------------------- annotated section
def doorsnede():
    s = Sketch(820, 440, seed=205, title="Dwarsdoorsnede van een put met "
                                         "mangat",
               desc="Het smalle mangat bovenaan wordt apart gerekend, de brede "
                    "hoofdput volgt de gewone berekening, en onderaan zit een "
                    "onbruikbare bodem.")
    s.title_text("Dwarsdoorsnede van een typische put", y=34)

    # lid
    s.rect(NL - 22, NT - 20, (NR + 22) - (NL - 22), 18, width=1.8,
           fill=s.CONCRETE, fill_opacity=0.45, hatch=45, hatch_spacing=7,
           hatch_color=s.CONCRETE, hatch_opacity=0.7)
    s.label(140, NT - 22, "deksel", to=P(NL - 18, NT - 12), anchor="end",
            weight="600")

    _put(s)

    # unusable bottom
    s.polyline([P(BL, BB - 26), P(BR, BB - 26), P(BR, BB), P(BL, BB)],
               close=True, width=1.4, stroke=s.INK_SOFT, hatch=-45,
               hatch_spacing=8, hatch_color=s.INK_SOFT, hatch_opacity=0.6)
    s.label(690, BB - 6, "onbruikbare bodem", to=P(BR - 40, BB - 13),
            anchor="end", size=13, color=s.INK_SOFT)

    # the "full" line
    s.line(BL, BT, BR, BT, stroke=s.BRAND, width=2.0, dash="9 6")
    s.label(690, BT - 10, "\"vol\"-niveau (100 %)", to=P(BR - 30, BT),
            anchor="end", weight="600", color=s.BRAND)

    s.label(185, 108, "mangat, smal", to=P(NL + 14, NT + 40), anchor="end",
            weight="600")
    s.text(185, 128, "0,50 m²", size=12.5, color=s.INK_SOFT, anchor="end")
    s.text(185, 146, "apart gerekend", size=12.5, color=s.INK_SOFT,
           anchor="end")

    s.label(185, 268, "hoofdput, breed", to=P(BL + 40, 290), anchor="end",
            weight="600")
    s.text(185, 288, "4,55 m²", size=12.5, color=s.INK_SOFT, anchor="end")
    s.text(185, 306, "gewone berekening", size=12.5, color=s.INK_SOFT,
           anchor="end")

    print(s.save(out("Mangat_Doorsnede.svg")))


if __name__ == "__main__":
    onder_100()
    boven_100()
    mangat_vorm()
    instellingen()
    doorsnede()
