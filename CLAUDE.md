# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Overview

WaterAlarm is a .NET 10 water level monitoring system for LoRaWAN sensors (production: https://wateralarm.be). It serves a web dashboard and REST API, sends email alarms, and ships PowerShell admin tooling.

## Commands

```bash
dotnet restore                                  # ~75s on cold cache — don't cancel
dotnet build --configuration Release --no-restore
dotnet test                                     # runs CoreTests + SiteTests (xUnit)
dotnet test CoreTests                           # single test project
dotnet test --filter "FullyQualifiedName~AggregatedMeasurementTest"   # single test class/method
```

Run the web app:

```bash
cd Site && dotnet run --environment Development
# Serves https://localhost:7189 and http://localhost:5088
# SQLite database is created and migrated automatically on startup
```

Build/use the PowerShell admin module (targets linux-x64 only, due to SQLite native libs):

```bash
cd Admin && dotnet publish --configuration Release
pwsh -c "Import-Module ./bin/Release/net10.0/linux-x64/publish/WaterAlarmAdmin.dll -Force"
# Cmdlets: Get-WAInfo, New-WAAccount, Add-WAAccountSensor, Set-WAAccountSensorAlarm, ...
# Requires the database to exist — run the web app once first
```

Deployment is via `scripts/deploy.sh` (wrapped by `deploy-wateralarm-dev.sh` / `-prd.sh`). CI is `.github/workflows/build.yml` (restore → build → test).

## Architecture

Solution projects: **Core** (shared library), **Site** (ASP.NET Core web app), **Admin** (PowerShell binary module), plus **CoreTests**/**SiteTests** (xUnit).

### Two data stores

- **SQLite via EF Core** (`Core/Repositories/WaterAlarmDbContext.cs`): configuration data — accounts, users, sensors, account-sensor links, alarm definitions. Migrations live in `Core/Migrations/` and apply automatically at Site startup. Database file: `Site/WaterAlarm.db`.
- **InfluxDB** (database `wateralarm`, via Vibrant.InfluxDB.Client): time-series sensor measurements. Measurements are *read* through `Core/Repositories/MeasurementRepositoryBase.cs`, with one repository subclass per sensor type (Level, Detect, Moisture, Thermometer). Sensor types are defined in `Core/Entities/Sensor.cs` (`SensorType` enum, also LevelPressure). Measurement data is written to InfluxDB outside this codebase — sensors deliver via The Things Network.

### CQRS with MediatR

All business logic lives in `Core/Commands/` and `Core/Queries/` as MediatR handlers. Both Site (pages/controllers) and Admin (cmdlets) send commands/queries through MediatR rather than touching the DbContext directly. DI wiring for all of Core is centralized in `Core/ServiceCollectionExtensions.AddWaterAlarmCore()`.

### Site

Razor Pages UI (`Site/Pages/`) plus REST API controllers (`Site/Controllers/`, e.g. `/api/a/{accountLink}/s/{sensorLink}`). Authentication: Google OAuth and email-link login for users, API-key auth (`Site/Authentication/ApiKeyAuthenticationHandler.cs`) for the API. There is also an MCP documentation endpoint (`Site/Controllers/McpController.cs`, route `/mcp`). Accounts and sensors are addressed by short random "links" (see `RegenerateAccountLinkCommandHandler` etc.), not by IDs.

### Alarms

Alarm checking is a Core command (`CheckAccountSensorAlarmsCommandHandler` / `CheckAllAccountSensorAlarmsCommandHandler`), triggered from the Admin module (`Invoke-WACheckAllAccountSensorAlarms`) — typically scheduled externally. Email is sent through `Core/Communication/Messenger.cs` (plain SMTP, template in `Core/Content/mail.html`).

### Configuration

`appsettings.json` + optional git-ignored `appsettings.Local.json` (both Site and Admin) for secrets/local overrides: connection string `WaterAlarmDb`, `Influx` section, SMTP credentials, Google auth, API keys.

## Notes

- `Docs/` contains user/ops documentation, largely in Dutch.
- Site project has a known CS1998 warning in TrendService.cs; it is expected.
