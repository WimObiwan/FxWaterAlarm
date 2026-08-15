#!/usr/bin/env python3
"""Cross-section of a rainwater well with an ultrasonic level sensor.

    python3 scripts/sketch/draw_regenput_sensor.py
"""

import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from sketchlib import P, Sketch  # noqa: E402

OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)),
                   "..", "..", "Docs", "_Tekeningen", "Regenput_Sensor.svg")

# --- geometry ---------------------------------------------------------------
W, H = 820, 560
GROUND = 170                     # ground level
WELL_L, WELL_R = 225, 565        # outer walls
WELL_T, WELL_B = 200, 478        # outer top slab / outer floor
IN_L, IN_R = 237, 553            # inner walls
IN_T, IN_B = 212, 466            # underside of the slab / inner floor
NECK_L, NECK_R = 348, 452        # manhole, outer
NECK_IL, NECK_IR = 360, 440      # manhole, inner
LID_T, LID_B = 148, 158          # lid
WATER = 368                      # water surface
SENSOR_L, SENSOR_R = 378, 416
SENSOR_T, SENSOR_B = 166, 190
SOIL_B = 505                     # bottom of the drawn soil

s = Sketch(W, H, seed=20260815, title="Regenput met ultrasone niveausensor",
           desc="Doorsnede van een regenput.  De sensor hangt onder het deksel "
                "en meet de afstand tot het wateroppervlak.")

s.title_text("Niveaumeting in een regenput", y=34)

# --- soil -------------------------------------------------------------------
for x0, y0, x1, y1 in (
    (40, GROUND, WELL_L, SOIL_B),          # left of the well
    (WELL_R, GROUND, W - 40, SOIL_B),      # right of the well
    (WELL_L, WELL_B, WELL_R, SOIL_B),      # underneath the well
    (WELL_L, GROUND, NECK_L, WELL_T),      # on top of the slab, left
    (NECK_R, GROUND, WELL_R, WELL_T),      # on top of the slab, right
):
    s.rect(x0, y0, x1 - x0, y1 - y0, stroke=s.SOIL, width=1.2, opacity=0.55,
           fill=s.SOIL, fill_opacity=0.10, hatch=-35, hatch_spacing=10,
           hatch_color=s.SOIL, hatch_opacity=0.45, passes=1)

s.line(40, GROUND, NECK_L, GROUND, stroke=s.SOIL, width=2.2)
s.line(NECK_R, GROUND, W - 40, GROUND, stroke=s.SOIL, width=2.2)
s.grass(55, NECK_L - 15, GROUND, count=11)
s.grass(NECK_R + 15, W - 55, GROUND, count=11)

# --- the well itself --------------------------------------------------------
outer = [P(WELL_L, WELL_T), P(NECK_L, WELL_T), P(NECK_L, LID_B),
         P(NECK_R, LID_B), P(NECK_R, WELL_T), P(WELL_R, WELL_T),
         P(WELL_R, WELL_B), P(WELL_L, WELL_B)]
inner = [P(IN_L, IN_T), P(NECK_IL, IN_T), P(NECK_IL, LID_B),
         P(NECK_IR, LID_B), P(NECK_IR, IN_T), P(IN_R, IN_T),
         P(IN_R, IN_B), P(IN_L, IN_B)]

s.polyline(outer, close=True, stroke=s.INK, width=2.0,
           fill=s.CONCRETE, fill_opacity=0.28, hatch=45, hatch_spacing=9,
           hatch_color=s.CONCRETE, hatch_opacity=0.7)
s.polyline(inner, close=True, stroke=s.INK, width=1.6,
           fill=s.PAPER, fill_opacity=1.0)

# --- water ------------------------------------------------------------------
s.polyline([P(IN_L, WATER), P(IN_R, WATER), P(IN_R, IN_B), P(IN_L, IN_B)],
           close=True, stroke=s.WATER, width=1.2, opacity=0.0, passes=1,
           fill=s.WATER, fill_opacity=0.16, hatch=18, hatch_spacing=11,
           hatch_color=s.WATER, hatch_opacity=0.45)
s.wave(IN_L, IN_R, WATER, amplitude=3.2, wavelength=52, width=2.0)

# --- lid --------------------------------------------------------------------
s.rect(NECK_L - 12, LID_T, (NECK_R + 12) - (NECK_L - 12), LID_B - LID_T,
       stroke=s.INK, width=1.8, fill=s.CONCRETE, fill_opacity=0.45,
       hatch=45, hatch_spacing=7, hatch_color=s.CONCRETE, hatch_opacity=0.7)

# --- sensor -----------------------------------------------------------------
for x in (SENSOR_L + 8, SENSOR_R - 8):
    s.line(x, LID_B, x, SENSOR_T, stroke=s.INK, width=1.3, passes=1)
s.rect(SENSOR_L, SENSOR_T, SENSOR_R - SENSOR_L, SENSOR_B - SENSOR_T,
       stroke=s.ACCENT, width=2.0, fill=s.ACCENT, fill_opacity=0.16)
s.ellipse((SENSOR_L + SENSOR_R) / 2, SENSOR_B, 7, 4, stroke=s.ACCENT,
          width=1.6, fill=s.ACCENT, fill_opacity=0.30)

# the ultrasonic cone
s.line(SENSOR_L + 9, SENSOR_B + 3, IN_L + 35, WATER - 6, stroke=s.ACCENT,
       width=1.2, passes=1, dash="5 6", opacity=0.55)
s.line(SENSOR_R - 9, SENSOR_B + 3, IN_R - 35, WATER - 6, stroke=s.ACCENT,
       width=1.2, passes=1, dash="5 6", opacity=0.55)

# what the sensor actually measures
s.dimension((SENSOR_L + SENSOR_R) / 2, SENSOR_B + 6,
            (SENSOR_L + SENSOR_R) / 2, WATER - 6,
            label="afstand tot het water", stroke=s.ACCENT, offset=12,
            font_size=13)

# --- what WaterAlarm derives from it ----------------------------------------
s.dimension(IN_L + 34, WATER + 4, IN_L + 34, IN_B, label="waterhoogte",
            stroke=s.BRAND, offset=11, font_size=13)

# --- transmission -----------------------------------------------------------
s.radio((NECK_L + NECK_R) / 2, LID_T - 4, count=3, start_r=13, step=10,
        spread=80)
s.label(505, 110, "LoRa of 5G", to=P(432, 126), color=s.BRAND, weight="600")

# --- labels -----------------------------------------------------------------
s.label(212, 110, "sensor", to=P(SENSOR_L + 4, SENSOR_T + 12), color=s.ACCENT,
        weight="600")
s.label(212, 146, "deksel / mangat", to=P(NECK_L - 6, LID_T + 5))
s.text(252, 250, "regenput", size=15, color=s.INK_SOFT, weight="600")
s.text(IN_R - 22, WATER + 34, "water", size=14, color=s.BRAND, weight="600",
       anchor="end")

s.caption("De sensor meet de afstand tot het wateroppervlak; WaterAlarm rekent "
          "dat om naar hoogte, volume en percentage.")

print(s.save(os.path.normpath(OUT)))
