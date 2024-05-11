using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public record AddDefaultSensorAlarmsCommand : IRequest
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
}

public class AddDefaultSensorAlarmsCommandHandler : IRequestHandler<AddDefaultSensorAlarmsCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AddDefaultSensorAlarmsCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(AddDefaultSensorAlarmsCommand request, CancellationToken cancellationToken)
    {
        var accountSensor =
            await _dbContext.Accounts
                .Where(a => a.Uid == request.AccountUid)
                .SelectMany(a => a.AccountSensors)
                .Include(@as => @as.Alarms)
                .SingleOrDefaultAsync(as2 => as2.Sensor.Uid == request.SensorUid, cancellationToken)
            ?? throw new AccountSensorNotFoundException("The account or sensor cannot be found.")
                { AccountUid = request.AccountUid, SensorUid = request.SensorUid };

        if (accountSensor.Alarms.Count > 0)
            throw new Exception("Adding default sensor alarms is only supported when no alarms are present");

        accountSensor.AddAlarm(new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Data,
            AlarmThreshold = 24.5
        });

        accountSensor.AddAlarm(new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.LevelFractionLow,
            AlarmThreshold = 25.0
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}