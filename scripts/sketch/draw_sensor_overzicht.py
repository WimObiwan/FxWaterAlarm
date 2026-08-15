#!/usr/bin/env python3
"""Drawings for Docs/Sensor_Overzicht.md.

    python3 scripts/sketch/draw_sensor_overzicht.py

One function per drawing, each with its own seed.
"""

import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from sketchlib import P, Sketch  # noqa: E402

OUT = os.path.normpath(os.path.join(os.path.dirname(os.path.abspath(__file__)),
                                    "..", "..", "Docs", "_Tekeningen"))


def out(name):
    return os.path.join(OUT, name)


# ---------------------------------------------------------------- LoRa chain
def verbinding_lora():
    s = Sketch(980, 360, seed=101, title="LoRa: sensor, gateway, internet",
               desc="De sensor stuurt via LoRa naar een gateway binnenshuis, "
                    "die via WiFi of ethernet naar het WaterAlarm-platform gaat.")
    s.title_text("LoRa — via een eigen gateway", y=34)

    y, h = 92, 92
    sx, gx, wx = 20, 270, 790
    ccx, cw = 615, 140
    mid = y + h / 2
    s.box(sx, y, 160, h, ["sensor", "op batterij"], fill=s.ACCENT,
          color=s.INK, stroke=s.ACCENT)
    s.box(gx, y, 180, h, ["gateway", "binnen, stroom", "+ WiFi of ethernet"],
          size=13, fill=s.BRAND)
    ty = s.cloud(ccx, mid, cw, 88, fill=s.BRAND)
    s.text(ccx, ty + 9, "internet", size=14, anchor="middle", weight="600",
           baseline="middle")
    s.box(wx, y, 160, h, ["WaterAlarm", "platform"], fill=s.BRAND)

    s.connect(sx + 162, mid, gx - 10, mid, "LoRa", "draadloos", stroke=s.BRAND)
    s.connect(gx + 182, mid, ccx - cw / 2 - 6, mid, stroke=s.BRAND)
    # this label does not fit in the gap, so it goes above the whole row
    s.text((gx + 182 + ccx - cw / 2) / 2, y - 12, "WiFi of ethernet", size=12,
           anchor="middle", weight="600", color=s.INK_SOFT)
    s.connect(ccx + cw / 2 + 6, mid, wx - 10, mid, stroke=s.BRAND)
    s.radio(sx + 140, y + 24, count=3, start_r=10, step=8, direction=-20,
            spread=70)

    # a second sensor on the same gateway
    y2 = 232
    s.box(sx, y2, 160, 70, ["tweede sensor"], size=13, stroke=s.ACCENT,
          fill=s.ACCENT, dash="7 5")
    s.arrow(sx + 164, y2 + 24, gx - 10, y + h - 8, stroke=s.BRAND, width=1.5,
            dash="7 5")
    s.text(300, y2 + 46, "Eén gateway bedient meerdere sensoren.", size=13.5,
           color=s.INK_SOFT, italic=True)

    print(s.save(out("Verbinding_LoRa.svg")))


# ------------------------------------------------------------------ 5G chain
def verbinding_5g():
    s = Sketch(880, 330, seed=102, title="5G: sensor, mobiel netwerk, platform",
               desc="De sensor met simkaart stuurt rechtstreeks over het "
                    "mobiele netwerk naar het WaterAlarm-platform.")
    s.title_text("5G (NB-IoT) — rechtstreeks via het mobiele netwerk", y=34)

    y, h = 100, 92
    sx, wx = 40, 690
    s.box(sx, y, 180, h, ["sensor", "op batterij", "met simkaart"], size=13,
          fill=s.ACCENT, stroke=s.ACCENT)

    # a mast instead of a box - it is what people picture
    mx, base, top = 430, 246, 96
    for d in (-1, 1):
        s.line(mx + d * 26, base, mx + d * 6, top, width=1.8)
    for t in (0.15, 0.42, 0.70):
        yy = base + (top - base) * t
        wdt = 26 - 20 * t
        s.line(mx - wdt, yy, mx + wdt, yy, width=1.3, passes=1)
    s.line(mx, top, mx, top - 16, width=1.8)
    s.radio(mx, top - 14, count=3, start_r=12, step=9, direction=-135, spread=70)
    s.radio(mx, top - 14, count=3, start_r=12, step=9, direction=-45, spread=70)
    s.text(mx, base + 26, "mobiel netwerk", size=13.5, anchor="middle",
           weight="600")
    s.text(mx, base + 46, "van de operator", size=13, anchor="middle",
           color=s.INK_SOFT)

    s.box(wx, y, 150, h, ["WaterAlarm", "platform"], fill=s.BRAND)

    s.connect(sx + 182, y + h / 2, mx - 60, y + h / 2 - 4, "5G (NB-IoT)",
              stroke=s.BRAND)
    s.connect(mx + 60, y + h / 2 - 4, wx - 8, y + h / 2, stroke=s.BRAND)

    s.caption("Geen gateway en geen internetverbinding ter plaatse nodig; "
              "de simkaart zit in het abonnement.")
    print(s.save(out("Verbinding_5G.svg")))


# ------------------------------------------------- ultrasonic versus pressure
def _tank(s, x, y, w, h, water_top):
    """Empty tank cross-section; returns the inner rectangle."""
    t = 11
    outer = [P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h)]
    s.polyline(outer, close=True, width=2.0, fill=s.CONCRETE,
               fill_opacity=0.28, hatch=45, hatch_spacing=9,
               hatch_color=s.CONCRETE, hatch_opacity=0.7)
    inner = [P(x + t, y + t), P(x + w - t, y + t), P(x + w - t, y + h - t),
             P(x + t, y + h - t)]
    s.polyline(inner, close=True, width=1.5, fill=s.PAPER, fill_opacity=1.0)
    il, ir, ib = x + t, x + w - t, y + h - t
    s.polyline([P(il, water_top), P(ir, water_top), P(ir, ib), P(il, ib)],
               close=True, width=1.0, opacity=0.0, passes=1, fill=s.WATER,
               fill_opacity=0.16, hatch=18, hatch_spacing=11,
               hatch_color=s.WATER, hatch_opacity=0.45)
    s.wave(il, ir, water_top, amplitude=3.0, wavelength=48, width=2.0)
    return il, ir, y + t, ib


def ultrasoon_druk():
    s = Sketch(900, 430, seed=103, title="Ultrasoon of druk",
               desc="Links meet een ultrasone sensor de afstand tot het "
                    "wateroppervlak, rechts meet een druksensor de waterdruk "
                    "boven een drukcel vlak boven de bodem.")
    s.title_text("Twee manieren om het waterniveau te meten", y=34)

    ty, th = 92, 250
    for x, w, name in ((40, 360, "ULTRASOON (DDS75)"), (500, 360, "DRUK (PS)")):
        s.text(x + w / 2, ty - 14, name, size=15, anchor="middle", weight="700")
    water = ty + 165

    # --- left: ultrasonic
    il, ir, it, ib = _tank(s, 40, ty, 360, th, water)
    cx = (il + ir) / 2
    s.rect(cx - 26, it - 4, 52, 28, stroke=s.ACCENT, width=2.0, fill=s.ACCENT,
           fill_opacity=0.16)
    s.ellipse(cx, it + 24, 8, 4, stroke=s.ACCENT, width=1.5, fill=s.ACCENT,
              fill_opacity=0.3)
    s.line(cx - 12, it + 28, il + 34, water - 8, stroke=s.ACCENT, width=1.2,
           passes=1, dash="5 6", opacity=0.55)
    s.line(cx + 12, it + 28, ir - 34, water - 8, stroke=s.ACCENT, width=1.2,
           passes=1, dash="5 6", opacity=0.55)
    s.dimension(cx, it + 30, cx, water - 6, label="afstand", stroke=s.ACCENT,
                offset=10)
    s.text(220, ty + th + 30, "meet de AFSTAND tot het water", size=13.5,
           anchor="middle", weight="600")
    s.text(220, ty + th + 50, "raakt het water nooit aan", size=13,
           anchor="middle", color=s.INK_SOFT)

    # --- right: pressure
    il, ir, it, ib = _tank(s, 500, ty, 360, th, water)
    cx = (il + ir) / 2
    s.rect(cx - 26, it - 4, 52, 28, stroke=s.ACCENT, width=2.0, fill=s.ACCENT,
           fill_opacity=0.16)
    cell_y = ib - 22
    s.line(cx, it + 26, cx, cell_y - 10, stroke=s.ACCENT, width=1.8)
    s.text(cx + 10, (it + cell_y) / 2, "kabel", size=12.5, color=s.INK_SOFT)
    s.rect(cx - 13, cell_y - 10, 26, 20, stroke=s.ACCENT, width=1.8,
           fill=s.ACCENT, fill_opacity=0.35)
    s.text(cx + 22, cell_y - 12, "drukcel", size=13, color=s.ACCENT,
           weight="600")
    s.dimension(il + 42, water + 6, il + 42, cell_y, label="waterkolom",
                stroke=s.BRAND, offset=10, font_size=13)
    s.text(680, ty + th + 30, "meet de DRUK boven de drukcel", size=13.5,
           anchor="middle", weight="600")
    s.text(680, ty + th + 50, "hangt permanent in het water", size=13,
           anchor="middle", color=s.INK_SOFT)

    print(s.save(out("Ultrasoon_Druk.svg")))


# ----------------------------------------------------- horizontal cylinder
def liggende_cilinder():
    s = Sketch(860, 340, seed=104, title="Vulhoogte in een liggende cilinder",
               desc="Bij 25 % hoogte zit er ongeveer 20 % inhoud in, bij 50 % "
                    "de helft, bij 75 % ongeveer 80 %.")
    s.title_text("Een liggende cilindertank loopt niet gelijkmatig vol", y=34)

    r, cy = 82, 178
    for cx, frac, hoogte, inhoud in ((160, 0.25, "25 % hoogte", "± 20 % inhoud"),
                                     (430, 0.50, "50 % hoogte", "50 % inhoud"),
                                     (700, 0.75, "75 % hoogte", "± 80 % inhoud")):
        ys = cy + r - 2 * r * frac                     # water surface
        half = math.sqrt(max(r * r - (ys - cy) ** 2, 0))
        a0 = math.atan2(ys - cy, half)                 # right intersection
        pts = [P(cx - half, ys)]
        steps = 26
        for i in range(steps + 1):
            a = (math.pi - a0) + (a0 - (math.pi - a0)) * i / steps
            pts.append(P(cx + r * math.cos(a), cy + r * math.sin(a)))
        s.polyline(pts, close=True, width=1.0, opacity=0.0, passes=1,
                   fill=s.WATER, fill_opacity=0.16, hatch=18, hatch_spacing=10,
                   hatch_color=s.WATER, hatch_opacity=0.45)
        s.circle(cx, cy, r, width=2.0)
        s.wave(cx - half, cx + half, ys, amplitude=2.6, wavelength=40, width=1.8)
        s.text(cx, cy - r - 16, hoogte, size=14, anchor="middle", weight="600")
        s.text(cx, cy + r + 30, inhoud, size=14, anchor="middle", weight="600",
               color=s.BRAND)

    s.caption("Onderaan en bovenaan is de tank smal, in het midden breed — "
              "de vulhoogte zegt dus niet rechtstreeks hoeveel liter er in zit.")
    print(s.save(out("Liggende_Cilinder.svg")))


# ------------------------------------------------------------ detection cable
def detectiekabel():
    s = Sketch(880, 370, seed=105, title="Waterdetectie met detectiekabel",
               desc="De detectiekabel ligt op de vloer en geeft alarm zodra er "
                    "op eender welk punt over de lengte water tegen komt.")
    s.title_text("De kabel detecteert over zijn volledige lengte", y=34)

    floor = 236
    s.line(40, floor, 840, floor, stroke=s.INK, width=2.0)
    s.rect(40, floor, 800, 34, stroke=s.CONCRETE, width=1.0, opacity=0.5,
           passes=1, hatch=-35, hatch_spacing=11, hatch_color=s.CONCRETE,
           hatch_opacity=0.6)

    # sensor against the wall
    s.line(40, 92, 40, floor, stroke=s.INK, width=2.0)
    s.box(58, 96, 150, 66, ["sensor"], fill=s.ACCENT, stroke=s.ACCENT)
    s.text(224, 122, "hoog tegen de muur,", size=12.5, color=s.INK_SOFT)
    s.text(224, 140, "buiten het water", size=12.5, color=s.INK_SOFT)
    s.line(133, 164, 133, 196, stroke=s.ACCENT, width=1.5, passes=1)
    s.line(133, 196, 190, floor - 6, stroke=s.ACCENT, width=1.5)

    # the cable itself
    s.line(190, floor - 6, 800, floor - 6, stroke=s.ACCENT, width=3.0)
    s.label(660, 150, "detectiekabel", to=P(560, floor - 12), color=s.ACCENT,
            weight="600")

    # two puddles, anywhere along the cable
    for px, txt in ((300, "water hier"), (640, "of hier")):
        s.ellipse(px, floor - 4, 62, 11, stroke=s.WATER, width=1.6,
                  fill=s.WATER, fill_opacity=0.3)
        s.wave(px - 58, px + 58, floor - 8, amplitude=2.2, wavelength=30,
               width=1.4)
        s.arrow(px, floor + 52, px, floor + 12, stroke=s.WATER, width=1.5)
        s.text(px, floor + 70, txt, size=13.5, anchor="middle", weight="600",
               color=s.WATER)

    s.caption("Water op eender welk punt van de kabel geeft alarm — de kabel "
              "meet niet op één plek.")
    print(s.save(out("Detectiekabel.svg")))


# ----------------------------------------------------------------- false echo
def valse_echo():
    s = Sketch(880, 400, seed=106, title="Valse echo op een buis",
               desc="Een buis in de put vangt de geluidspuls op, waardoor de "
                    "sensor de afstand tot de buis meet in plaats van tot het "
                    "water.")
    s.title_text("Valse echo: de puls botst op een buis", y=34)

    x, y, w, h = 250, 78, 380, 270
    water = y + 190
    il, ir, it, ib = _tank(s, x, y, w, h, water)
    cx = (il + ir) / 2

    # sensor, mounted off-centre so the cone catches the pipe
    sx = cx + 30
    s.rect(sx - 26, it - 4, 52, 28, stroke=s.ACCENT, width=2.0, fill=s.ACCENT,
           fill_opacity=0.16)
    s.ellipse(sx, it + 24, 8, 4, stroke=s.ACCENT, width=1.5, fill=s.ACCENT,
              fill_opacity=0.3)

    # the pipe, hanging free in the well
    px = il + 44
    s.rect(px, it, 26, (ib - it) - 4, stroke=s.INK, width=1.6,
           fill=s.CONCRETE, fill_opacity=0.5)
    s.label(120, 128, "losse buis", to=P(px + 4, it + 46), anchor="end")

    # the pulse: down, then reflected off the pipe
    s.line(sx - 8, it + 28, px + 34, it + 96, stroke=s.ACCENT, width=1.4,
           passes=1, dash="5 6", opacity=0.7)
    s.arrow(px + 34, it + 100, px + 30, it + 104, stroke=s.ACCENT, width=1.4)
    s.arrow(px + 40, it + 104, sx - 4, it + 34, stroke=s.ACCENT, width=1.6)
    s.text(px + 58, it + 132, "echo op de buis", size=13, color=s.ACCENT,
           weight="600")

    s.dimension(ir - 46, it + 30, ir - 46, water - 6, label="echte afstand",
                stroke=s.BRAND, offset=-104, font_size=12.5)

    s.caption("De sensor meet dan de afstand tot de buis in plaats van tot "
              "het wateroppervlak.")
    print(s.save(out("Valse_Echo.svg")))


# ------------------------------------------------------------ decision trees
def keuze_verbinding():
    s = Sketch(880, 400, seed=107, title="LoRa of 5G kiezen",
               desc="Beslissingsboom: zonder internet ter plaatse is 5G de "
                    "enige optie; diep in beton of onder een metalen deksel "
                    "is LoRa betrouwbaarder.")
    s.title_text("LoRa of 5G?", y=34)

    qx, qy, qw, qh = 60, 60, 400, 60
    s.box(qx, qy, qw, qh, ["Is er WiFi of ethernet", "bij de meetplaats?"],
          size=14, fill=s.BRAND, fill_opacity=0.08)

    def uitkomst(x, y, naam, toelichting, kleur=None):
        s.text(x, y - 4, naam, size=15, weight="700", color=kleur or s.BRAND)
        s.text(x, y + 16, toelichting, size=12, color=s.INK_SOFT)

    # nee -> 5G
    s.connect(qx + qw + 8, qy + qh / 2, 596, qy + qh / 2, "nee")
    uitkomst(612, qy + qh / 2, "5G (NB-IoT)", "er is geen alternatief",
             kleur=s.ACCENT)

    # ja -> three cases underneath
    s.connect(qx + 90, qy + qh, qx + 90, qy + qh + 40, "ja")
    rows = [
        (196, "Diep in een betonnen put,\nof onder een metalen deksel?", "LoRa",
         "komt beter door beton, verbruikt minder"),
        (272, "Meerdere sensoren op dezelfde\nlocatie (of gedeeld met buren)?",
         "LoRa", "één gateway bedient ze allemaal"),
        (344, "Anders?", "beide gaan",
         "5G is het eenvoudigst te installeren"),
    ]
    for ry, vraag, naam, toelichting in rows:
        lines = vraag.split("\n")
        h = 52 if len(lines) > 1 else 42
        s.box(90, ry - h / 2, 380, h, lines, size=13, weight="500",
              stroke=s.INK_SOFT, width=1.4)
        s.arrow(478, ry, 596, ry, stroke=s.INK_SOFT, width=1.5)
        uitkomst(612, ry, naam, toelichting,
                 kleur=None if naam == "LoRa" else s.INK)

    print(s.save(out("Keuze_Verbinding.svg")))


def keuze_meetgrootheid():
    s = Sketch(880, 370, seed=108, title="Welke sensor voor wat",
               desc="Waterniveau in een gewone put: ultrasoon.  Put met "
                    "buizen of een smalle schacht, en stookolie: druk.  "
                    "Verder bodemvocht en detectiekabel.")
    s.title_text("Wat wil je meten?", y=34)

    rows = [
        ("Waterniveau, gewone open put", "ultrasoon", "het goedkoopst"),
        ("Waterniveau, put met buizen,\nkabels of een smalle schacht", "druk", ""),
        ("Stookolie in een tank", "druk", ""),
        ("Bodemvochtigheid", "bodemvochtsensor", ""),
        ("Lek of overstroming", "detectiekabel", ""),
    ]
    y = 92
    for vraag, uitkomst, extra in rows:
        lines = vraag.split("\n")
        h = 54 if len(lines) > 1 else 44
        s.box(50, y, 380, h, lines, size=13, weight="500", stroke=s.INK_SOFT,
              width=1.4)
        s.arrow(440, y + h / 2, 560, y + h / 2, stroke=s.INK_SOFT, width=1.5)
        s.text(576, y + h / 2 + (0 if not extra else -3), uitkomst, size=15,
               weight="700", color=s.BRAND, baseline="middle" if not extra else "auto")
        if extra:
            s.text(576, y + h / 2 + 17, extra, size=12, color=s.INK_SOFT)
        y += h + 12

    print(s.save(out("Keuze_Meetgrootheid.svg")))


if __name__ == "__main__":
    verbinding_lora()
    verbinding_5g()
    ultrasoon_druk()
    liggende_cilinder()
    detectiekabel()
    valse_echo()
    keuze_verbinding()
    keuze_meetgrootheid()
