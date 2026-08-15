# sketch — hand-drawn illustrations for `Docs/`

Small Python helper that emits "sketchy" SVG: every straight segment becomes a slightly
wobbly bezier drawn twice, the way a pen doubles back over a line. The look is deliberately
informal — these drawings explain a product to a customer, they are not construction plans.

Output goes to `Docs/_Tekeningen/*.svg` and is committed. Pages embed it with a raw `<img>`
tag (Bootstrap has no global `img { max-width }`, so keep the `img-fluid` class):

```html
<img src="_Tekeningen/Regenput_Sensor.svg" alt="..." class="img-fluid" style="max-width: 620px;">
```

## Regenerate

```bash
python3 scripts/sketch/draw_regenput_sensor.py     # writes Docs/_Tekeningen/Regenput_Sensor.svg
```

No dependencies beyond the standard library. The wobble comes from a seeded
`random.Random`, so regenerating without changing the script produces a byte-identical file
and an empty git diff. **Edit the script, never the generated SVG.**

Preview a change:

```bash
google-chrome --headless --disable-gpu --screenshot=/tmp/x.png --window-size=830,570 \
  file://$PWD/Docs/_Tekeningen/Regenput_Sensor.svg
```

## Adding a drawing

Copy `draw_regenput_sensor.py`, keep the same shape: geometry constants at the top, then
background → structure → content → annotation → labels. Give each drawing its **own seed**
so two drawings do not wobble identically.

## Style rules

These keep the set looking like one family:

* **Paper background, not transparent.** The site has a dark theme; an opaque
  `Sketch.PAPER` background keeps the drawing readable in both.
* **Colours have a meaning**, always the same one:

  | Colour | Constant | Used for |
  |---|---|---|
  | ink grey | `INK` | outlines of everything physical |
  | soft grey | `INK_SOFT` | leader lines, dimensions, secondary text |
  | brand blue | `BRAND` | WaterAlarm itself: transmission, derived values |
  | water blue | `WATER` | water, waves |
  | amber | `ACCENT` | the sensor and what it measures |
  | brown | `SOIL` | earth |
  | warm grey | `CONCRETE` | walls, lids, tanks |
  | green | `GREEN` | grass, plants |

* **Fills are hatched, not solid** (`hatch=<angle>`), at a low opacity. A solid fill kills
  the hand-drawn effect.
* **Labels are Dutch and lowercase**, except product names (`DDS75-LB`, `LoRa`, `5G`).
* Use `label(..., to=P(x, y))` for a leader line, `dimension(...)` for anything measured.
* One title (`title_text`) and at most one `caption` per drawing; the surrounding page text
  does the explaining, the drawing only has to be recognisable.
* Canvas ≈ 800×550 user units. Larger drawings stay readable because SVG scales, but the
  font sizes in `sketchlib` are tuned for roughly this size.

## Library

`sketchlib.py`, one class:

* shapes — `line`, `polyline`, `rect`, `circle`, `ellipse`, `arc`
* texture — `hatch`, `wave`, `grass`, `radio`
* annotation — `arrow`, `dimension`, `text`, `label`, `title_text`, `caption`
* output — `save(path)`, `to_svg()`

`polyline`/`rect` take `fill`, `fill_opacity`, `hatch`, `hatch_spacing`, `hatch_color`,
`dash`, `passes` and `bow` (0 = straight, 1 = default wobble). Draw order is painter's
order, so cover something by drawing a `PAPER`-filled shape over it.
