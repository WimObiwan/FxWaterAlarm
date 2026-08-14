## Feature Request Draft: Support Fuel And Horizontal Tanks

### Goal
Support fuel tanks while keeping full backward compatibility for existing water setups.

The same physical sensors are used, but two additional configuration concerns must be supported:
1. Liquid density for pressure-based level calculations.
2. Tank geometry for volume calculations, including horizontal cylinders.

### Scope
- In scope:
    - Sensor configuration extensions.
    - Volume calculation updates.
    - Optional calibration for non-perfect cylinders (for example tanks with end caps).
- Out of scope:
    - New sensor hardware integrations.
    - Changes to historical measurements already stored.

## Requirements

### R1. Add Density For Pressure Sensors
- Add optional field: `densityKgPerM3`.
- Applies only to pressure sensor configuration.
- Unit: kg/m^3.
- Default when missing/null: `1000` (water).
- Backward compatibility:
    - Existing pressure sensors without this field must keep working.
    - Existing results for water setups must remain unchanged.

### R2. Add Geometry For Volume Conversion
- Add field: `geometry`.
- Applies to both pressure and water-level sensor configurations.
- Allowed values:
    - `Default`
    - `HorizontalCylinder`
- Default when missing/null: `Default`.
- Backward compatibility:
    - Existing sensors without this field must keep current behavior (= `Default`).

### R3. Dimensions For Horizontal Cylinder
When `geometry = HorizontalCylinder`, require:
- `diameterMm` (unit: mm)
- `lengthMm` (unit: mm)

Validation:
- `diameterMm > 0`
- `lengthMm > 0`

### R4. Horizontal Cylinder Volume Formula
For `geometry = HorizontalCylinder`, compute volume from liquid height using:

`V = L * (R^2 * arccos((R - h)/R) - (R - h) * sqrt(2*R*h - h^2))`

Definitions:
- `L` = cylinder length
- `R` = radius = `diameter / 2`
- `h` = liquid height

Input handling:
- Clamp `h` to `[0, diameter]` before calculation.

Units:
- If dimensions are in mm, computed volume is in mm^3.
- Convert to liters with: `liters = mm^3 / 1_000_000`.

### R5. Pressure Calculation Uses Density
For pressure sensors:
1. Convert pressure to liquid height using configured density.
2. Use resulting height in geometry-specific volume calculation.

If density is missing, use `1000 kg/m^3`.

### R6. Optional Capacity Calibration (Compensation For End Caps)
Real tanks may not be perfect cylinders. Add optional field:
- `nominalCapacityLiters`

Applies when `geometry = HorizontalCylinder`.

Behavior:
1. Compute geometric full volume at `h = diameter`: `VGeomFullLiters`.
2. If `nominalCapacityLiters` is set and > 0, compute correction factor:
     - `k = nominalCapacityLiters / VGeomFullLiters`
3. Use corrected volume:
     - `VCorrected = k * VGeom(h)`
4. Clamp output to `[0, nominalCapacityLiters]`.

This compensates for tanks with rounded or capped ends while preserving level curve shape.

## Non-Functional Requirements
- N1. Backward compatibility is mandatory for existing sensors/configurations.
- N2. No breaking API changes for clients that do not send new fields.
- N3. Validation errors must be explicit and human-readable.

## Proposed Data Model Additions
- `densityKgPerM3` (nullable number, pressure sensors only)
- `geometry` (enum/string, default `Default`)
- `diameterMm` (nullable number, required for `HorizontalCylinder`)
- `lengthMm` (nullable number, required for `HorizontalCylinder`)
- `nominalCapacityLiters` (nullable number, optional for `HorizontalCylinder`)

## Acceptance Criteria
1. Existing sensors with no new fields produce unchanged values.
2. Pressure sensors without `densityKgPerM3` behave as `1000 kg/m^3`.
3. `HorizontalCylinder` configuration is rejected when diameter or length is missing/invalid.
4. Horizontal cylinder volume calculation returns correct values at:
     - Empty (`h = 0`) => 0 liters
     - Half level (`h = R`) => 50% of geometric full volume
     - Full (`h = diameter`) => geometric full volume (or nominal capacity when calibration is set)
5. With `nominalCapacityLiters` set, full-scale reported volume equals nominal capacity.
6. Output is always clamped to valid range (never negative, never above maximum).
7. API/admin surfaces persist and return all new fields.

## Example (From Real Use Case)
- Diameter: `1900 mm`
- Length: `4500 mm`
- Geometric volume is about `12,700 L`
- Real known capacity is `10,000 L`

With calibration:
- `k = 10,000 / 12,700 ~= 0.7874`
- Reported volume curve follows cylinder geometry but scales to real capacity.

## Implementation Notes
- Keep naming consistent: use `HorizontalCylinder` (not `cilinder`).
- Prefer nullable fields + defaults for migration safety.
- Add unit tests for:
    - Backward compatibility defaults
    - Geometry formula edge points (`h = 0`, `h = R`, `h = D`)
    - Calibration factor behavior
    - Validation failures

---------------------

Simplification after testing:
- Remove `Nominal Capacity` and `Cilinder length`.  Use the existing `Capacity` setting to deduct the cilinder length.
- The percentage in the blue circle in the app should be based on the calculated volume, not the liquid height, to reflect the actual fill level more accurately for horizontal tanks.

To keep the code cleaner: you can update the EF migration(s), don't create new ones.  I will reset the database, so the migration will work correctly.

---------------------

Next simplification after testing:
Also remove the `Cilinder diameter`.  The diameter can be calculated:
- In case of Pressure sensors: (distance 100%) + ((distance 0%) ?? 0)
- In case of water level sensors: (distance 0%) - (distance 100%)

Again, to keep the code cleaner: you can update the EF migration(s), don't create new ones.  I will reset the database, so the migration will work correctly.

Implemented on 2026-05-31:
- `Cilinder diameter` input/storage is removed from UI/API/Admin and entity persistence.
- Diameter is now derived in code from configured distance settings:
    - Pressure sensors: `(distance 100%) + ((distance 0%) ?? 0)`
    - Water level sensors: `(distance 0%) - (distance 100%)`
- Horizontal cylinder validation now depends on a positive derived diameter and a positive `Capacity`.

---------------------

Visualization update after testing:
Problem:
  There is a DiagramViewComponent that shows the tank fill level.
  This is designed for vertical tanks, so it uses the liquid height percentage to fill the diagram.
  The diagram is not correct for tanks with `HorizontalCylinder` geometry.
I suggest to:
- Show a different diagram for `HorizontalCylinder` geometry.  
- Most accurate would be to use a circle instead of a rectangle.  If possible, also with some "manhole" on top.
- Add same info as on the default diagram.
- use a different component for this, to keep the code cleaner and avoid too many conditionals in the existing diagram component.