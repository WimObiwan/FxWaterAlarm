using Core.Exceptions;
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