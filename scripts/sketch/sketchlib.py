"""sketchlib - a tiny hand-drawn ("sketchy") SVG library for WaterAlarm drawings.

Every straight segment is replaced by a slightly wobbly cubic bezier and drawn
twice, the way a pen doubles back over a line.  The wobble comes from a seeded
random generator, so regenerating a drawing gives byte-identical SVG and a clean
git diff.

Usage::

    from sketchlib import Sketch, P

    s = Sketch(800, 520, seed=7, title="Regenput met sensor")
    s.rect(100, 100, 200, 120, stroke=s.INK, fill=s.WATER, hatch=45)
    s.text(200, 90, "regenput", anchor="middle")
    s.save("Docs/_Tekeningen/Regenput_Sensor.svg")

Coordinates are plain SVG user units, y grows downwards.
"""

from __future__ import annotations

import math
import random
from typing import Iterable, Sequence

Point = tuple[float, float]


def P(x: float, y: float) -> Point:
    return (float(x), float(y))


class Sketch:
    """A drawing surface that emits hand-drawn looking SVG."""

    # --- palette -----------------------------------------------------------
    PAPER = "#FCFBF7"       # background, so the drawing survives a dark theme
    INK = "#33383D"         # main outline
    INK_SOFT = "#7C848C"    # secondary lines, dimension lines
    BRAND = "#2257BF"       # WaterAlarm blue (sampled from the favicon)
    WATER = "#4E8FE0"       # water fill / waves
    ACCENT = "#E08A1E"      # the sensor and what it measures
    SOIL = "#8C6239"        # earth
    CONCRETE = "#B9B2A6"    # walls, lids, structures
    GREEN = "#5B8C3A"       # grass, plants

    # single quotes: this string ends up inside a double-quoted XML attribute
    FONT = ("ui-rounded, 'Segoe UI', 'Trebuchet MS', "
            "system-ui, -apple-system, sans-serif")

    def __init__(self, width: float, height: float, seed: int = 1,
                 title: str = "", desc: str = "", background: str | None = None,
                 roughness: float = 1.0):
        self.width = float(width)
        self.height = float(height)
        self.title = title
        self.desc = desc
        self.background = self.PAPER if background is None else background
        self.roughness = roughness
        self.rng = random.Random(seed)
        self.parts: list[str] = []

    # --- low level ---------------------------------------------------------
    def _j(self, amount: float) -> float:
        """A random offset, scaled by the global roughness."""
        return self.rng.uniform(-amount, amount) * self.roughness

    def _f(self, v: float) -> str:
        """Format a number: short, stable, no '-0'."""
        s = f"{v:.2f}".rstrip("0").rstrip(".")
        return "0" if s in ("-0", "") else s

    def _bezier(self, a: Point, b: Point, bow: float) -> str:
        """One wobbly stroke from a to b as a cubic bezier."""
        (x1, y1), (x2, y2) = a, b
        dx, dy = x2 - x1, y2 - y1
        length = math.hypot(dx, dy) or 1.0
        # jitter grows with the length of the segment, but not without bound
        w = min(1.0 + length * 0.045, 5.0) * bow
        # perpendicular unit vector: this is what makes the line bow
        px, py = -dy / length, dx / length
        c1 = (x1 + dx * 0.3 + px * self._j(w) + self._j(w * 0.4),
              y1 + dy * 0.3 + py * self._j(w) + self._j(w * 0.4))
        c2 = (x1 + dx * 0.7 + px * self._j(w) + self._j(w * 0.4),
              y1 + dy * 0.7 + py * self._j(w) + self._j(w * 0.4))
        e = (x2 + self._j(w * 0.5), y2 + self._j(w * 0.5))
        f = self._f
        return (f"M{f(x1 + self._j(w * 0.5))} {f(y1 + self._j(w * 0.5))} "
                f"C{f(c1[0])} {f(c1[1])} {f(c2[0])} {f(c2[1])} {f(e[0])} {f(e[1])}")

    def _stroke_path(self, d: str, stroke: str, width: float,
                     opacity: float = 1.0, dash: str | None = None) -> None:
        attrs = [f'd="{d}"', f'stroke="{stroke}"',
                 f'stroke-width="{self._f(width)}"', 'fill="none"',
                 'stroke-linecap="round"', 'stroke-linejoin="round"']
        if opacity != 1.0:
            attrs.append(f'stroke-opacity="{self._f(opacity)}"')
        if dash:
            attrs.append(f'stroke-dasharray="{dash}"')
        self.parts.append("<path " + " ".join(attrs) + " />")

    def _rough_segments(self, pts: Sequence[Point], stroke: str, width: float,
                        passes: int = 2, opacity: float = 1.0,
                        dash: str | None = None, bow: float = 1.0) -> None:
        """Draw a polyline `passes` times, each time with a fresh wobble."""
        for i in range(passes):
            d = " ".join(self._bezier(pts[k], pts[k + 1], bow)
                         for k in range(len(pts) - 1))
            self._stroke_path(d, stroke, width, opacity * (1.0 if i == 0 else 0.7),
                              dash)

    # --- primitives --------------------------------------------------------
    def line(self, x1, y1, x2, y2, stroke: str | None = None, width: float = 1.6,
             passes: int = 2, dash: str | None = None, opacity: float = 1.0,
             bow: float = 1.0) -> None:
        self._rough_segments([P(x1, y1), P(x2, y2)], stroke or self.INK, width,
                             passes, opacity, dash, bow)

    def polyline(self, pts: Sequence[Point], stroke: str | None = None,
                 width: float = 1.6, close: bool = False, passes: int = 2,
                 dash: str | None = None, opacity: float = 1.0,
                 fill: str | None = None, fill_opacity: float = 1.0,
                 hatch: float | None = None, hatch_spacing: float = 7,
                 hatch_color: str | None = None, hatch_width: float = 1.0,
                 hatch_opacity: float = 0.55, bow: float = 1.0) -> None:
        pts = [P(*p) for p in pts]
        ring = pts + [pts[0]] if close else pts
        if fill:
            d = ("M" + " L".join(f"{self._f(x)} {self._f(y)}" for x, y in pts)
                 + " Z")
            self.parts.append(
                f'<path d="{d}" fill="{fill}" '
                f'fill-opacity="{self._f(fill_opacity)}" stroke="none" />')
        if hatch is not None:
            self.hatch(pts, angle=hatch, spacing=hatch_spacing,
                       stroke=hatch_color or stroke or self.INK,
                       width=hatch_width, opacity=hatch_opacity)
        self._rough_segments(ring, stroke or self.INK, width, passes, opacity,
                             dash, bow)

    def rect(self, x, y, w, h, **kw) -> None:
        self.polyline([P(x, y), P(x + w, y), P(x + w, y + h), P(x, y + h)],
                      close=True, **kw)

    def ellipse(self, cx, cy, rx, ry, stroke: str | None = None,
                width: float = 1.6, passes: int = 2, fill: str | None = None,
                fill_opacity: float = 1.0, dash: str | None = None,
                opacity: float = 1.0, steps: int | None = None,
                start: float = 0.0, end: float = 2 * math.pi,
                close: bool = True, bow: float = 0.7) -> None:
        if steps is None:
            # enough segments that a big circle does not read as a polygon
            steps = max(16, int(math.pi * (rx + ry) / 11))
        pts = []
        for i in range(steps + 1):
            a = start + (end - start) * i / steps
            pts.append(P(cx + rx * math.cos(a), cy + ry * math.sin(a)))
        if fill:
            self.parts.append(
                f'<ellipse cx="{self._f(cx)}" cy="{self._f(cy)}" '
                f'rx="{self._f(rx)}" ry="{self._f(ry)}" fill="{fill}" '
                f'fill-opacity="{self._f(fill_opacity)}" stroke="none" />')
        if not close:
            pts = pts[:-1] if end - start >= 2 * math.pi else pts
        self._rough_segments(pts, stroke or self.INK, width, passes, opacity,
                             dash, bow)

    def circle(self, cx, cy, r, **kw) -> None:
        self.ellipse(cx, cy, r, r, **kw)

    def arc(self, cx, cy, r, start_deg: float, end_deg: float, **kw) -> None:
        self.ellipse(cx, cy, r, r, start=math.radians(start_deg),
                     end=math.radians(end_deg), close=False, **kw)

    # --- fills and textures ------------------------------------------------
    def hatch(self, pts: Sequence[Point], angle: float = 45, spacing: float = 7,
              stroke: str | None = None, width: float = 1.0,
              opacity: float = 0.55) -> None:
        """Fill a polygon with parallel pen strokes at `angle` degrees."""
        pts = [P(*p) for p in pts]
        a = math.radians(angle)
        ca, sa = math.cos(-a), math.sin(-a)
        rot = [(x * ca - y * sa, x * sa + y * ca) for x, y in pts]
        ys = [p[1] for p in rot]
        y = math.floor(min(ys) / spacing) * spacing + spacing
        ca2, sa2 = math.cos(a), math.sin(a)
        while y < max(ys):
            xs = []
            for i in range(len(rot)):
                (x1, y1), (x2, y2) = rot[i], rot[(i + 1) % len(rot)]
                if (y1 <= y < y2) or (y2 <= y < y1):
                    xs.append(x1 + (x2 - x1) * (y - y1) / (y2 - y1))
            xs.sort()
            for i in range(0, len(xs) - 1, 2):
                x_a, x_b = xs[i] + spacing * 0.15, xs[i + 1] - spacing * 0.15
                if x_b - x_a > 1.0:
                    p1 = (x_a * ca2 - y * sa2, x_a * sa2 + y * ca2)
                    p2 = (x_b * ca2 - y * sa2, x_b * sa2 + y * ca2)
                    self._rough_segments([p1, p2], stroke or self.INK, width,
                                         passes=1, opacity=opacity, bow=1.3)
            y += spacing

    def wave(self, x1: float, x2: float, y: float, amplitude: float = 3.5,
             wavelength: float = 26, stroke: str | None = None,
             width: float = 1.6, passes: int = 2, opacity: float = 1.0) -> None:
        """A wavy line - a water surface."""
        pts = []
        n = max(int((x2 - x1) / (wavelength / 6)), 6)
        for i in range(n + 1):
            t = i / n
            x = x1 + (x2 - x1) * t
            pts.append(P(x, y + amplitude * math.sin(2 * math.pi * (x - x1) / wavelength)))
        self._rough_segments(pts, stroke or self.WATER, width, passes, opacity,
                             bow=0.4)

    def grass(self, x1: float, x2: float, y: float, count: int = 14,
              height: float = 9, stroke: str | None = None) -> None:
        """Little tufts along a ground line."""
        stroke = stroke or self.GREEN
        for i in range(count):
            x = x1 + (x2 - x1) * (i + 0.5) / count + self._j(4)
            h = height * self.rng.uniform(0.6, 1.2)
            for lean in (-0.45, 0.0, 0.45):
                self._rough_segments(
                    [P(x, y), P(x + h * lean, y - h)], stroke, 1.1,
                    passes=1, opacity=0.85, bow=1.5)

    def radio(self, cx: float, cy: float, count: int = 3, start_r: float = 9,
              step: float = 8, direction: float = -90, spread: float = 60,
              stroke: str | None = None, width: float = 1.4) -> None:
        """Concentric arcs, the universal 'this thing transmits' symbol."""
        for i in range(count):
            self.arc(cx, cy, start_r + i * step, direction - spread / 2,
                     direction + spread / 2, stroke=stroke or self.BRAND,
                     width=width, passes=1, opacity=0.9 - i * 0.15)

    def cloud(self, cx: float, cy: float, w: float, h: float,
              stroke: str | None = None, width: float = 1.6,
              fill: str | None = None, fill_opacity: float = 0.10) -> float:
        """A lumpy cloud - the internet, a network, anything 'out there'.

        Fits the box (cx, cy) +- (w/2, h/2).  Returns the y at which a caption
        sits optically centred inside it.
        """
        stroke = stroke or self.INK
        bottom = cy + h / 2
        left, right = cx - w / 2, cx + w / 2
        # (position along the bottom, radius as a fraction of the largest bump)
        bumps = [(0.15, 0.62), (0.42, 1.0), (0.69, 0.80), (0.90, 0.55)]
        big = h / 1.55                      # so the tallest bump fills h
        top = bottom
        for t, rf in bumps:
            r = big * rf
            top = min(top, bottom - r * 0.55 - r)
        if fill:
            ry = (bottom - top) / 2
            self.parts.append(
                f'<ellipse cx="{self._f(cx)}" cy="{self._f(bottom - ry * 0.92)}" '
                f'rx="{self._f(w * 0.46)}" ry="{self._f(ry * 0.96)}" '
                f'fill="{fill}" fill-opacity="{self._f(fill_opacity)}" '
                f'stroke="none" />')
        for t, rf in bumps:
            r = big * rf
            bx = left + (right - left) * t
            self.arc(bx, bottom - r * 0.55, r, 185, 355, stroke=stroke,
                     width=width, passes=1)
        self._rough_segments([P(left + w * 0.06, bottom), P(right - w * 0.06, bottom)],
                             stroke, width, passes=1)
        return bottom - (bottom - top) * 0.42

    # --- blocks ------------------------------------------------------------
    def box(self, x, y, w, h, lines: str | Sequence[str] = (), size: float = 14,
            stroke: str | None = None, color: str | None = None,
            fill: str | None = None, fill_opacity: float = 0.10,
            width: float = 1.8, weight: str = "600", dash: str | None = None,
            leading: float = 1.35, hatch: float | None = None,
            hatch_color: str | None = None) -> tuple[float, float]:
        """A labelled box.  Returns its centre, handy for drawing arrows."""
        stroke = stroke or self.INK
        self.rect(x, y, w, h, stroke=stroke, width=width, fill=fill,
                  fill_opacity=fill_opacity, dash=dash, hatch=hatch,
                  hatch_color=hatch_color, hatch_spacing=8, hatch_opacity=0.5)
        if isinstance(lines, str):
            lines = [lines]
        cx, cy = x + w / 2, y + h / 2
        step = size * leading
        top = cy - step * (len(lines) - 1) / 2
        for i, line in enumerate(lines):
            self.text(cx, top + i * step, line, size=size,
                      color=color or self.INK, anchor="middle", weight=weight,
                      baseline="middle")
        return cx, cy

    def connect(self, x1, y1, x2, y2, label: str = "", sub: str = "",
                stroke: str | None = None, dash: str | None = None,
                size: float = 12.5, color: str | None = None,
                width: float = 1.6) -> None:
        """An arrow between two boxes, with an optional label above it."""
        stroke = stroke or self.INK
        self.arrow(x1, y1, x2, y2, stroke=stroke, width=width, dash=dash)
        mx, my = (x1 + x2) / 2, (y1 + y2) / 2
        color = color or self.INK_SOFT
        if abs(y2 - y1) > abs(x2 - x1):
            # vertical arrow: labels go beside it, not on top of it
            if label:
                self.text(mx + 10, my, label, size=size, color=color,
                          weight="600", baseline="middle")
            if sub:
                self.text(mx + 10, my + 17, sub, size=size - 0.5, color=color)
            return
        if label:
            self.text(mx, my - 9, label, size=size, anchor="middle",
                      color=color, weight="600")
        if sub:
            self.text(mx, my + 19, sub, size=size - 0.5, anchor="middle",
                      color=color)

    # --- annotation --------------------------------------------------------
    def arrow_head(self, x: float, y: float, angle_deg: float,
                   size: float = 8, stroke: str | None = None,
                   width: float = 1.5) -> None:
        a = math.radians(angle_deg)
        for spread in (math.radians(160), math.radians(-160)):
            self._rough_segments(
                [P(x, y), P(x + size * math.cos(a + spread),
                            y + size * math.sin(a + spread))],
                stroke or self.INK, width, passes=1, bow=0.5)

    def arrow(self, x1, y1, x2, y2, stroke: str | None = None,
              width: float = 1.5, dash: str | None = None,
              both: bool = False, size: float = 8, opacity: float = 1.0) -> None:
        stroke = stroke or self.INK
        self.line(x1, y1, x2, y2, stroke=stroke, width=width, passes=1,
                  dash=dash, opacity=opacity)
        ang = math.degrees(math.atan2(y2 - y1, x2 - x1))
        self.arrow_head(x2, y2, ang, size, stroke, width)
        if both:
            self.arrow_head(x1, y1, ang + 180, size, stroke, width)

    def dimension(self, x1, y1, x2, y2, label: str = "",
                  stroke: str | None = None, offset: float = 0,
                  font_size: float = 13, tick: float = 5) -> None:
        """A double arrow with end ticks and a label beside it."""
        stroke = stroke or self.INK_SOFT
        self.arrow(x1, y1, x2, y2, stroke=stroke, width=1.3, both=True, size=7)
        dx, dy = x2 - x1, y2 - y1
        length = math.hypot(dx, dy) or 1.0
        px, py = -dy / length * tick, dx / length * tick
        for (x, y) in ((x1, y1), (x2, y2)):
            self._rough_segments([P(x - px, y - py), P(x + px, y + py)],
                                 stroke, 1.3, passes=1)
        if label:
            mx, my = (x1 + x2) / 2, (y1 + y2) / 2
            vertical = abs(dy) > abs(dx)
            self.text(mx + (offset if vertical else 0),
                      my + (0 if vertical else offset),
                      label, anchor="start" if vertical else "middle",
                      size=font_size, color=stroke,
                      baseline="middle" if vertical else "auto")

    def text(self, x, y, s: str, size: float = 14, color: str | None = None,
             anchor: str = "start", weight: str = "500", italic: bool = False,
             baseline: str = "auto", opacity: float = 1.0) -> None:
        esc = (s.replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;"))
        attrs = [f'x="{self._f(x)}"', f'y="{self._f(y)}"',
                 f'font-family="{self.FONT}"', f'font-size="{self._f(size)}"',
                 f'fill="{color or self.INK}"', f'text-anchor="{anchor}"',
                 f'font-weight="{weight}"']
        if italic:
            attrs.append('font-style="italic"')
        if baseline != "auto":
            attrs.append(f'dominant-baseline="{baseline}"')
        if opacity != 1.0:
            attrs.append(f'fill-opacity="{self._f(opacity)}"')
        self.parts.append("<text " + " ".join(attrs) + f">{esc}</text>")

    def label(self, x, y, s: str, to: Point | None = None, size: float = 14,
              color: str | None = None, anchor: str = "start",
              weight: str = "500") -> None:
        """A text label, optionally with a thin leader line to what it names."""
        color = color or self.INK
        self.text(x, y, s, size=size, color=color, anchor=anchor, weight=weight)
        if to is not None:
            # start the leader just outside the text box, on the side that
            # faces whatever the label points at
            pad = 7
            w = 0.55 * size * len(s)
            left, right = {"start": (x, x + w), "end": (x - w, x),
                           "middle": (x - w / 2, x + w / 2)}[anchor]
            sy = y - size * 0.32
            if to[0] > right:
                sx = right + pad
            elif to[0] < left:
                sx = left - pad
            else:
                sx = (left + right) / 2
                sy = y + pad if to[1] > y else y - size - pad * 0.5
            self.line(sx, sy, to[0], to[1], stroke=self.INK_SOFT, width=1.1,
                      passes=1, opacity=0.8)
            self.circle(to[0], to[1], 2.2, stroke=self.INK_SOFT, width=1.1,
                        passes=1, fill=self.INK_SOFT)

    def title_text(self, s: str, x: float | None = None, y: float = 30,
                   size: float = 20) -> None:
        self.text(self.width / 2 if x is None else x, y, s, size=size,
                  color=self.INK, anchor="middle" if x is None else "start",
                  weight="700")

    def caption(self, s: str, x: float | None = None, y: float | None = None,
                size: float = 12.5) -> None:
        self.text(self.width / 2 if x is None else x,
                  self.height - 14 if y is None else y, s, size=size,
                  color=self.INK_SOFT, anchor="middle" if x is None else "start",
                  italic=True)

    # --- output ------------------------------------------------------------
    def to_svg(self) -> str:
        head = [
            f'<svg xmlns="http://www.w3.org/2000/svg" '
            f'viewBox="0 0 {self._f(self.width)} {self._f(self.height)}" '
            f'width="{self._f(self.width)}" height="{self._f(self.height)}" '
            f'role="img" aria-label="{self.title}">'
        ]
        if self.title:
            head.append(f"  <title>{self.title}</title>")
        if self.desc:
            head.append(f"  <desc>{self.desc}</desc>")
        if self.background:
            head.append(f'  <rect width="100%" height="100%" rx="6" '
                        f'fill="{self.background}" />')
        body = "\n".join("  " + p for p in self.parts)
        return "\n".join(head) + "\n" + body + "\n</svg>\n"

    def save(self, path: str) -> str:
        svg = self.to_svg()
        with open(path, "w", encoding="utf-8") as f:
            f.write(svg)
        return path


def polygon_points(*pairs: float) -> list[Point]:
    """polygon_points(x1, y1, x2, y2, ...) -> [(x1, y1), (x2, y2), ...]"""
    it: Iterable[float] = iter(pairs)
    return [P(x, y) for x, y in zip(it, it)]
