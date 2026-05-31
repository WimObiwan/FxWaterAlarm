using Core.Exceptions;
using Core.Entities;
using Core.Repositories;
using Core.Util;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Commands;

public record UpdateAccountSensorCommand : IRequest
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
    public Optional<bool> Disabled { get; init; }
    public Optional<int> Order { get; init; }
    public Optional<string> Name { get; init; }
    public Optional<int?> DistanceMmEmpty { get; init; }
    public Optional<int?> DistanceMmFull { get; init; }
    public Optional<int?> UnusableHeightMm { get; init; }
    public Optional<int?> CapacityL { get; init; }
    public Optional<bool> AlertsEnabled { get; init; }
    public Optional<bool> NoMinMaxConstraints { get; init; }
    public Optional<double?> ManholeAreaM2 { get; init; }
    public Optional<double?> DensityKgPerM3 { get; init; }
    public Optional<TankGeometry> Geometry { get; init; }
}

public class UpdateAccountSensorCommandHandler : IRequestHandler<UpdateAccountSensorCommand>
{
    private readonly WaterAlarmDbContext _dbContext;
    private readonly ILogger _logger;

    public UpdateAccountSensorCommandHandler(WaterAlarmDbContext dbContext, ILogger<UpdateAccountSensorCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(UpdateAccountSensorCommand request, CancellationToken cancellationToken)
    {
        var accountSensor =
            await _dbContext.Accounts
                .Where(a => a.Uid == request.AccountUid)
                .SelectMany(a => a.AccountSensors)
                .SingleOrDefaultAsync(as2 => as2.Sensor.Uid == request.SensorUid, cancellationToken)
            ?? throw new AccountSensorNotFoundException("The account or sensor cannot be found.")
                { AccountUid = request.AccountUid, SensorUid = request.SensorUid };

        if (request.Disabled is { Specified: true })
            accountSensor.Disabled = request.Disabled.Value;
        if (request.Order is { Specified: true })
            accountSensor.Order = request.Order.Value;
        if (request.Name is { Specified: true })
            accountSensor.Name = request.Name.Value;
        if (request.DistanceMmEmpty is { Specified: true })
            accountSensor.DistanceMmEmpty = request.DistanceMmEmpty.Value;
        if (request.DistanceMmFull is { Specified: true })
            accountSensor.DistanceMmFull = request.DistanceMmFull.Value;
        if (request.UnusableHeightMm is { Specified: true })
            accountSensor.UnusableHeightMm = request.UnusableHeightMm.Value;
        if (request.CapacityL is { Specified: true })
            accountSensor.CapacityL = request.CapacityL.Value;
        if (request.AlertsEnabled is { Specified: true})
            accountSensor.AlertsEnabled = request.AlertsEnabled.Value;
        if (request.NoMinMaxConstraints is { Specified: true})
            accountSensor.NoMinMaxConstraints = request.NoMinMaxConstraints.Value;
        if (request.ManholeAreaM2 is { Specified: true})
            accountSensor.ManholeAreaM2 = request.ManholeAreaM2.Value;
        if (request.DensityKgPerM3 is { Specified: true })
            accountSensor.DensityKgPerM3 = request.DensityKgPerM3.Value;
        if (request.Geometry is { Specified: true })
            accountSensor.Geometry = request.Geometry.Value;

        if (accountSensor.DensityKgPerM3 is { } densityKgPerM3 && densityKgPerM3 <= 0.0)
            throw new InvalidOperationException("DensityKgPerM3 must be greater than zero.");

        if (accountSensor.DensityKgPerM3.HasValue && accountSensor.Sensor.Type != Entities.SensorType.LevelPressure)
            throw new InvalidOperationException("DensityKgPerM3 can only be configured for pressure level sensors.");

        if (accountSensor.Geometry == TankGeometry.HorizontalCylinder)
        {
            if (accountSensor.Sensor.Type != Entities.SensorType.Level && accountSensor.Sensor.Type != Entities.SensorType.LevelPressure)
                throw new InvalidOperationException("HorizontalCylinder geometry is only supported for level sensors.");

            int diameterMm;
            if (accountSensor.Sensor.Type == Entities.SensorType.LevelPressure)
            {
                if (accountSensor.DistanceMmFull is not { } distanceMmFull || distanceMmFull <= 0)
                    throw new InvalidOperationException("HorizontalCylinder geometry requires DistanceMmFull > 0 for pressure sensors.");

                if (accountSensor.DistanceMmEmpty is { } distanceMmEmpty && distanceMmEmpty < 0)
                    throw new InvalidOperationException("HorizontalCylinder geometry requires DistanceMmEmpty >= 0 for pressure sensors.");

                diameterMm = distanceMmFull + (accountSensor.DistanceMmEmpty ?? 0);
            }
            else
            {
                if (accountSensor.DistanceMmEmpty is not { } distanceMmEmpty || distanceMmEmpty <= 0)
                    throw new InvalidOperationException("HorizontalCylinder geometry requires DistanceMmEmpty > 0 for level sensors.");

                if (accountSensor.DistanceMmFull is not { } distanceMmFull || distanceMmFull < 0)
                    throw new InvalidOperationException("HorizontalCylinder geometry requires DistanceMmFull >= 0 for level sensors.");

                diameterMm = distanceMmEmpty - distanceMmFull;
            }

            if (diameterMm <= 0)
                throw new InvalidOperationException("HorizontalCylinder geometry requires valid distance settings to derive a positive diameter.");

            if (accountSensor.CapacityL is not { } capacityL || capacityL <= 0)
                throw new InvalidOperationException("CapacityL must be greater than zero when using HorizontalCylinder geometry.");
        }

        if (request.Order is { Specified: true})
        {
            _logger.LogInformation("Updating order of account sensor {AccountSensor} to {Order}", 
                accountSensor.Name, request.Order.Value);

            // var account = await _dbContext.Accounts.Where(a => a.Uid == request.AccountUid)
            var account = await _dbContext.Entry(accountSensor).Reference(a => a.Account)
                .Query()
                .Include(a => a.AccountSensors)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new AccountNotFoundException("The account cannot be found.")
                    { AccountUid = request.AccountUid };

            ResetAccountSensorOrderHelper.ResetOrder(_logger, account, accountSensor);
        }
        
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}