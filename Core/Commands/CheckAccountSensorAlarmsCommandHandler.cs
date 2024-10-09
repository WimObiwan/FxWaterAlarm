using Core.Communication;
using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Commands;

public record CheckAccountSensorAlarmsCommand : IRequest
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
}

public class CheckAccountSensorAlarmsCommandHandler : CheckAccountSensorAlarmsCommandHandlerBase, IRequestHandler<CheckAccountSensorAlarmsCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public CheckAccountSensorAlarmsCommandHandler(WaterAlarmDbContext dbContext, IMeasurementLevelRepository measurementLevelRepository, IMessenger messenger, 
        ILogger<CheckAccountSensorAlarmsCommandHandler> logger)
        : base(dbContext, measurementLevelRepository, messenger, logger)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(CheckAccountSensorAlarmsCommand request, CancellationToken cancellationToken)
    {
        var accountSensor =
            await _dbContext.Accounts
                .Where(a => a.Uid == request.AccountUid)
                .SelectMany(a => a.AccountSensors)
                .Where(@as => @as.Sensor.Uid == request.SensorUid)
                .Include(@as => @as.Sensor)
                .Include(@as => @as.Account)
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AccountSensorNotFoundException("The accountsensor cannot be found.") 
            { AccountUid = request.AccountUid, SensorUid = request.SensorUid };

        await CheckAccountSensorAlarms(accountSensor, cancellationToken);
    }
}

