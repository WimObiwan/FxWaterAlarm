# WaterAlarm - Water Level Monitoring System

WaterAlarm is a .NET 10.0 water level monitoring system for LoRaWAN sensors that provides web-based dashboards, email alarms, and PowerShell administrative tools. The system uses Entity Framework Core with SQLite for data storage and serves both a public web interface and REST API.

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Bootstrap, Build, and Test the Repository
- Install .NET 10.0 SDK if not available: `wget https://dot.net/v1/dotnet-install.sh && bash dotnet-install.sh --version 10.0`
- Navigate to repository root: `/home/runner/work/FxWaterAlarm/FxWaterAlarm`
- Restore dependencies: `dotnet restore` -- takes 75 seconds. NEVER CANCEL. Set timeout to 120+ seconds.
- Build solution: `dotnet build --configuration Release --no-restore` -- takes 17 seconds. NEVER CANCEL. Set timeout to 60+ seconds.
- Run tests: `dotnet test --no-restore --verbosity normal` -- takes 6 seconds. Set timeout to 30+ seconds.

### Run the Web Application
- ALWAYS run the bootstrapping steps first (restore and build).
- Development mode: `cd Site && dotnet run --environment Development`
- Production build: `dotnet publish --configuration Release`
- Application starts on: `https://localhost:7189` and `http://localhost:5088` (redirects to HTTPS)
- Database migrations run automatically on first startup (creates SQLite database with all tables)
- NEVER CANCEL: Application startup takes ~5 seconds including database migration. Set timeout to 30+ seconds.

### PowerShell Admin Tools
- Build admin module: `cd Admin && dotnet publish --configuration Release`
- Navigate to: `cd Admin/bin/Release/net10.0/linux-x64/publish`
- Import module: `pwsh -c "Import-Module ./WaterAlarmAdmin.dll -Force"``
- Available commands: `Get-WAInfo`, `New-WAAccount`, `Add-WAAccountSensor`, `Set-WAAccountSensorAlarm`, etc.
- Admin tools require the database to be initialized (run web app first to create database)

## Validation

### Manual Testing Scenarios
- ALWAYS run through these validation steps after making changes:
1. **Build Validation**: Run `dotnet restore && dotnet build --configuration Release` - must complete without errors
2. **Test Validation**: Run `dotnet test` - all 10 tests must pass
3. **Web App Validation**: Start web app with `cd Site && dotnet run`, verify it starts and serves content at `https://localhost:7189`
4. **Database Validation**: Confirm SQLite database is created automatically with all tables via migrations
5. **PowerShell Module Validation**: Build and import admin module, run `Get-Command -Module WaterAlarmAdmin` to verify cmdlets load

### API Testing
- Test API endpoint: `curl -k https://localhost:7189/api/a/demo/s/i5WOmUdoO0` (when web app is running)
- API provides JSON data for sensor readings, trends, and measurements

### Deployment Testing
- Validate deploy script syntax: `bash -n scripts/deploy.sh`
- Deploy script handles: Site, Admin PowerShell module, and service restart

## Project Structure

### Key Projects
- **Core/**: Shared library with entities, repositories, commands, queries (MediatR CQRS pattern)
- **Site/**: ASP.NET Core web application with Razor Pages, API endpoints
- **Admin/**: PowerShell module for administrative tasks (targets linux-x64)
- **CoreTests/**: xUnit test project with 10 tests covering core functionality

### Important Files and Locations
- Solution file: `WaterAlarm.sln`
- Database: SQLite with automatic migrations (`Site/WaterAlarm.db`)
- Configuration: `Site/appsettings.json`, `Site/appsettings.Development.json`
- Launch settings: `Site/Properties/launchSettings.json`
- CI/CD pipeline: `.github/workflows/build.yml`
- Deployment script: `scripts/deploy.sh`
- Documentation: `Docs/` (includes API docs, version info, integration guides)

### Common Navigation Paths
- Main web interface: Navigate to Site project for UI components and controllers
- Business logic: Navigate to Core project for entities, repositories, and commands
- Admin operations: Navigate to Admin project for PowerShell cmdlets
- Database models: Check Core/Entities/ and Core/Migrations/
- API endpoints: Check Site/Controllers/ and Site/Pages/

## Build Timing Expectations

**CRITICAL**: Set appropriate timeouts for all build commands. DO NOT use default timeouts.

- `dotnet restore`: 75 seconds actual, use 120+ second timeout. NEVER CANCEL.
- `dotnet build`: 17 seconds actual, use 60+ second timeout. NEVER CANCEL.
- `dotnet test`: 6 seconds actual, use 30+ second timeout.
- `dotnet publish`: ~20 seconds, use 60+ second timeout. NEVER CANCEL.
- Web app startup: 5 seconds including migrations, use 30+ second timeout.

## Technologies and Dependencies

### Core Technologies
- .NET 10.0 with C# (nullable reference types enabled)
- Entity Framework Core 10.0 with SQLite provider
- ASP.NET Core with Razor Pages
- MediatR for CQRS pattern
- PowerShell Standard Library for admin cmdlets

### Key NuGet Packages
- Entity Framework Core (SQLite, Design, Relational)
- MediatR for command/query handling
- Serilog for logging
- InfluxDB client for time-series data
- PowerShell Standard Library
- xUnit for testing

## Known Issues and Warnings

### Build Warnings
- Site project: CS1998 warning about async method without await in TrendService.cs - this is expected and can be ignored
- Admin project requires linux-x64 runtime due to SQLite native library constraints

### Database Notes
- SQLite database is created automatically on first web app startup
- Database file location: `Site/WaterAlarm.db`
- Migrations run automatically - no manual intervention needed
- PowerShell admin tools require database to exist (run web app first)

## CI/CD Information

### GitHub Actions
- Workflow file: `.github/workflows/build.yml`
- Runs on: Ubuntu latest with .NET 10.0.x
- Steps: Checkout → Setup .NET → Restore → Build → Test
- Always run `dotnet restore && dotnet build --configuration Release && dotnet test` before pushing changes

### Deployment
- Production deployment via `scripts/deploy.sh` 
- Requires parameters: .NET version, target server, paths, service name
- Handles: Site web app, PowerShell admin module, service restart
- Example: `./scripts/deploy.sh "net10.0" "user@server" "/var/www/site" "kestrel-service" "/opt/admin" "/opt/console"`

## Development Workflow
- Always build and test changes: `dotnet restore && dotnet build --configuration Release && dotnet test`
- For web changes: Start web app and test in browser at `https://localhost:7189`
- For admin changes: Build and import PowerShell module, test cmdlets
- For database changes: Verify migrations apply correctly on web app startup
- Always validate that CI pipeline requirements are met before committing