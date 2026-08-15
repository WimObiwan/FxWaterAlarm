#!/usr/bin/env python3
"""Drawings for the device pages in Docs/Sensor_Nodes/.

    python3 scripts/sketch/draw_sensor_nodes.py

The two "inmeten" drawings follow the platform exactly (Core/Entities/
AccountSensor.cs): for an ultrasonic sensor the height is
`afstand leeg - afstand vol`, for a pressure sensor it is
`afstand leeg + afstand vol`.
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from sketchlib import P, Sketch  # noqa: E402

OUT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                    "..", "..", "Docs", "_Tekeningen"))


def out(name):
    return os.path.join(OUT, name)


def _put(s, x, y, w, h, water, lid=True):
    """Well cross-section with walls; returns the inner rectangle."""
    t = 12
    s.polyline([P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h)], close=True,
               width=2.0, fill=s.CONCRETE, fill_opacity=0.28, hatch=45,
               hatch_spacing=9, hatch_color=s.CONCRETE, hatch_opacity=0.7)
    il, ir, it, ib = x + t, x + w - t, y + t, y + h - t
    s.polyline([P(il, it), P(ir, it), P(ir, ib), P(il, ib)], close=True,
               width=1.5, fill=s.PAPER, fill_opacity=1.0)
    s.polyline([P(il, water), P(ir, water), P(ir, ib), P(il, ib)], close=True,
               width=1.0, opacity=0.0, passes=1, fill=s.WATER,
               fill_opacity=0.16, hatch=18, hatch_spacing=11,
               hatch_color=s.WATER, hatch_opacity=0.45)
    s.wave(il, ir, water, amplitude=3.0, wavelength=50, width=2.0)
    if lid:
        s.rect(x + w / 2 - 70, y - 16, 140, 14, width=1.6, fill=s.CONCRETE,
               fill_opacity=0.45, hatch=45, hatch_spacing=7,
               hatch_color=s.CONCRETE, hatch_opacity=0.7)
    return il, ir, it, ib


# ------------------------------------------------------- measuring, ultrasonic
def inmeten_ultrasoon():
    s = Sketch(880, 480, seed=301, title="Inmeten van een put met een "
                                         "ultrasone sensor",
               desc="Beide afstanden meet je vanaf de sensor: tot het "
                    "waterniveau dat 0 % moet zijn, en tot het niveau dat "
                    "100 % moet zijn.")
    s.title_text("Wat opmeten bij een ultrasone sensor", y=34)

    x, y, w, h = 250, 96, 380, 320
    vol, leeg = 190, 386                      # the 100 % and 0 % levels
    il, ir, it, ib = _put(s, x, y, w, h, 268)
    cx = (il + ir) / 2

    # sensor under the lid
    s.rect(cx - 26, y - 2, 52, 26, stroke=s.ACCENT, width=2.0, fill=s.ACCENT,
           fill_opacity=0.16)
    s.ellipse(cx, y + 24, 8, 4, stroke=s.ACCENT, width=1.5, fill=s.ACCENT,
              fill_opacity=0.3)
    s.label(200, y + 4, "sensor", to=P(cx - 24, y + 10), anchor="end",
            color=s.ACCENT, weight="600")

    # the two reference levels
    for ly, txt, kleur in ((vol, "100 %", s.BRAND), (leeg, "0 %", s.BRAND)):
        s.line(il, ly, ir, ly, stroke=kleur, width=1.8, dash="9 6")
        s.text(ir - 10, ly - 8, txt, size=13, anchor="end", weight="700",
               color=kleur)

    # the two distances, both measured from the sensor
    s.dimension(il + 46, y + 28, il + 46, vol, stroke=s.ACCENT, offset=0)
    s.text(il + 56, (y + 28 + vol) / 2, "vol", size=13.5, weight="700",
           color=s.ACCENT, baseline="middle")
    s.dimension(ir - 46, y + 28, ir - 46, leeg, stroke=s.ACCENT, offset=0)
    s.text(ir - 56, (y + 28 + leeg) / 2, "leeg", size=13.5, weight="700",
           color=s.ACCENT, anchor="end", baseline="middle")

    s.text(660, 150, "Afstand sensor tot water", size=13, weight="600")
    s.text(660, 168, "wanneer vol (100%)", size=13, weight="600")
    s.text(660, 188, "bv. 800 mm", size=12.5, color=s.INK_SOFT)
    s.text(660, 250, "Afstand sensor tot water", size=13, weight="600")
    s.text(660, 268, "wanneer leeg (0%)", size=13, weight="600")
    s.text(660, 288, "bv. 3.000 mm", size=12.5, color=s.INK_SOFT)
    s.text(660, 350, "Capaciteit", size=13, weight="600")
    s.text(660, 370, "bv. 10.000 liter", size=12.5, color=s.INK_SOFT)

    # unusable water at the bottom sits just above the 0 % level
    s.polyline([P(il, leeg - 26), P(ir, leeg - 26), P(ir, leeg), P(il, leeg)],
               close=True, width=1.2, stroke=s.INK_SOFT, passes=1, hatch=-45,
               hatch_spacing=8, hatch_color=s.INK_SOFT, hatch_opacity=0.55)
    s.label(200, leeg - 4, "onbruikbare waterhoogte", to=P(il + 30, leeg - 13),
            anchor="end", size=12.5, color=s.INK_SOFT)
    s.text(200, leeg + 14, "onderaan (optioneel)", size=12.5, anchor="end",
           color=s.INK_SOFT)

    s.caption("Meet met de put open, en meet allebei vanaf de sensor.")
    print(s.save(out("Inmeten_Ultrasoon.svg")))


# -------------------------------------------------------- measuring, pressure
def inmeten_druk():
    s = Sketch(880, 480, seed=302, title="Inmeten van een put met een "
                                         "druksensor",
               desc="Meet vanaf de onderkant van de druksensor tot de "
                    "bovenkant van de put, en tot de bodem.  Samen geven ze "
                    "de hoogte van de put.")
    s.title_text("Wat opmeten bij een druksensor", y=34)

    x, y, w, h = 250, 96, 380, 320
    il, ir, it, ib = _put(s, x, y, w, h, 250)
    cx = (il + ir) / 2
    cell_y = ib - 30                          # bottom of the pressure cell

    s.line(cx, y + 6, cx, cell_y - 22, stroke=s.ACCENT, width=2.0)
    s.rect(cx - 14, cell_y - 22, 28, 22, stroke=s.ACCENT, width=2.0,
           fill=s.ACCENT, fill_opacity=0.35)
    s.text(cx + 14, 168, "kabel", size=13, color=s.ACCENT, weight="600")
    s.label(210, cell_y - 30, "druksensor", to=P(cx - 16, cell_y - 12),
            anchor="end", color=s.ACCENT, weight="600")
    s.text(210, cell_y - 12, "zweeft vlak boven de bodem", size=12.5,
           anchor="end", color=s.INK_SOFT)

    # top of the well, and the two distances from the bottom of the cell
    s.line(il, it, ir, it, stroke=s.BRAND, width=1.6, dash="9 6")
    s.text(ir - 10, it + 18, "bovenkant van de put", size=12, anchor="end",
           weight="600", color=s.BRAND)
    s.dimension(il + 52, it, il + 52, cell_y, stroke=s.ACCENT, offset=0)
    s.text(il + 62, (it + cell_y) / 2, "tot de bovenkant", size=13,
           weight="700", color=s.ACCENT, baseline="middle")
    # the gap under the cell is small, so it gets its own arrow from outside
    s.dimension(cx + 36, cell_y, cx + 36, ib, stroke=s.ACCENT, tick=4)
    s.arrow(ir + 92, cell_y + 46, cx + 46, cell_y + 12, stroke=s.ACCENT,
            width=1.5)
    s.text(ir + 100, cell_y + 50, "tot de bodem", size=13, weight="700",
           color=s.ACCENT)

    s.text(660, 150, "Afstand sensor tot water", size=13, weight="600")
    s.text(660, 168, "wanneer vol (100%)", size=13, weight="600")
    s.text(660, 188, "bv. 1.990 mm", size=12.5, color=s.INK_SOFT)
    s.text(660, 232, "Afstand sensor tot water", size=13, weight="600")
    s.text(660, 250, "wanneer leeg (0%)", size=13, weight="600")
    s.text(660, 270, "bv. 10 mm — optioneel", size=12.5, color=s.INK_SOFT)
    s.text(660, 314, "Capaciteit", size=13, weight="600")
    s.text(660, 334, "bv. 10.000 liter", size=12.5, color=s.INK_SOFT)

    s.text(440, 452, "samen = de hoogte van de put, bv. 2.000 mm", size=13.5,
           anchor="middle", weight="700", color=s.BRAND)
    print(s.save(out("Inmeten_Druk.svg")))


# ------------------------------------------------------------- soil moisture
def montage_bodemvocht():
    s = Sketch(860, 380, seed=303, title="Montage van de bodemvochtsensor",
               desc="De behuizing blijft boven de grond, de drie pinnen van de "
                    "sonde gaan volledig en verticaal in de grond, op de "
                    "diepte van de wortels.")
    s.title_text("De sonde in de grond zetten", y=34)

    ground = 168
    s.rect(40, ground, 780, 152, stroke=s.SOIL, width=1.2, opacity=0.55,
           fill=s.SOIL, fill_opacity=0.10, hatch=-35, hatch_spacing=10,
           hatch_color=s.SOIL, hatch_opacity=0.45, passes=1)
    s.line(40, ground, 820, ground, stroke=s.SOIL, width=2.2)
    s.grass(60, 330, ground, count=8)
    s.grass(520, 800, ground, count=9)

    # housing above ground, probe below
    hx = 430
    s.rect(hx - 46, ground - 96, 92, 78, stroke=s.ACCENT, width=2.0,
           fill=s.ACCENT, fill_opacity=0.16)
    s.text(hx, ground - 52, "sensor", size=13, anchor="middle", weight="600")
    s.line(hx, ground - 18, hx, ground + 8, stroke=s.ACCENT, width=1.8)
    for d in (-14, 0, 14):
        s.line(hx + d, ground + 8, hx + d, ground + 92, stroke=s.ACCENT,
               width=2.2)
        s.line(hx + d, ground + 92, hx + d - 3, ground + 100, stroke=s.ACCENT,
               width=1.6, passes=1)
    s.line(hx - 18, ground + 8, hx + 18, ground + 8, stroke=s.ACCENT, width=2.0)

    s.label(250, ground - 74, "behuizing boven de grond", to=P(hx - 50, ground - 66),
            anchor="end", weight="600")
    # the explanation stays above the ground line, where the paper is clean
    s.label(566, ground - 84, "de drie pinnen volledig", to=P(hx + 20, ground + 46),
            weight="600")
    s.text(566, ground - 66, "en verticaal in de grond,", size=12.5,
           color=s.INK_SOFT)
    s.text(566, ground - 48, "op de diepte van de wortels", size=12.5,
           color=s.INK_SOFT)

    s.dimension(hx - 90, ground, hx - 90, ground + 92, label="wortelzone",
                stroke=s.BRAND, offset=-88, font_size=12.5)

    s.caption("Druk de aarde daarna goed aan: luchtholtes tussen de pinnen en "
              "de grond geven een te lage vochtmeting.")
    print(s.save(out("Montage_Bodemvocht.svg")))


if __name__ == "__main__":
    inmeten_ultrasoon()
    inmeten_druk()
    montage_bodemvocht()
