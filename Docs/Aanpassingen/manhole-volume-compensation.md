---
title: Manhole Volume Compensation
date: 2026-02-01
---

# Manhole Volume Compensation

## Summary

Added the ability to compensate water volume calculations for the manhole (access shaft) above a well. When the water level rises above 100% of the well's usable capacity, the system now accounts for the smaller cross-sectional area of the manhole to produce a more accurate volume reading.

## Problem

Previously, when the water level exceeded the configured "full" level, the volume calculation either capped at 100% or extrapolated using the well's cross-sectional area. In practice, wells often have a narrower manhole shaft above the main reservoir. Water rising into the manhole displaces far less volume per mm of height than water in the main well. Without compensation, overflow volume was significantly overestimated.

## Solution

A new optional property **Manhole area (m²)** (`ManholeAreaM2`) has been added to the account sensor configuration. When set, it adjusts the volume calculation for water levels above 100%.

### How It Works

1. When the real level fraction is **≤ 1.0** (at or below full), the calculation is unchanged.
2. When the real level fraction **exceeds 1.0** and a manhole area is configured:
   - The overflow height in mm is calculated from the fraction above 100%.
   - The manhole volume is computed as:
     $$\text{manholeVolumeL} = \text{overflowHeightMm} \times \text{manholeAreaM2}$$
     (Since 1 m² × 1 mm = 1 L, the manhole area in m² directly gives L/mm.)
   - The total volume becomes:
     $$\text{totalVolumeL} = \text{usableCapacityL} + \text{manholeVolumeL}$$
   - The displayed level fraction is adjusted accordingly.
3. When the real level fraction **exceeds 1.0** and **no** manhole area is configured, the level is capped at 100%.

### Example

A well with 10,000 L usable capacity, 2,000 mm usable height, and a manhole area of 1.0 m²:

- Measurement shows 2,500 mm usable height → 125% real level.
- Overflow: 500 mm above full.
- Main well volume: 10,000 L (at 100%).
- Manhole volume: 500 mm × 1.0 m² = 500 L.
- Total volume: 10,500 L → displayed as ~105.5%.

## Changes

### Database

- Added nullable `ManholeAreaM2` column (type `REAL`) to the `AccountSensor` table via migration `20260201190000_AddAccountSensorManholeAreaM2`.

### Core

- **`AccountSensor.ManholeAreaM2`** — new nullable `double` property on the entity.
- **`MeasurementDistance`** — added `ApplyManholeCompensation` methods that adjust `LevelFraction`, `LevelFractionIncludingUnusableHeight`, and `WaterL` when overflow is detected.
- **`UpdateAccountSensorCommand`** — accepts the new `ManholeAreaM2` parameter.

### Web UI (Site)

- Added a **Manhole area** input field (with m² suffix) to the sensor settings form.
- Input supports decimal values with comma-to-dot normalization.
- Localized labels: English — *Manhole area*, Dutch — *Mangat oppervlakte*.

### Admin (PowerShell)

- **`Set-WAAccountSensor`** — added `-ManholeAreaM2` parameter.
- **`Get-WAAccountSensor`** — includes `ManholeAreaM2` in output.

### Tests

- Unit tests for `MeasurementDistance` manhole compensation logic (zero area caps at 1.0, null capacity returns raw fraction).
- Integration tests for `MeasurementLevelEx` with both `Level` and `LevelPressure` sensor types.
- Site validation tests for the manhole area input (negative values, unparseable input, valid values, null).
