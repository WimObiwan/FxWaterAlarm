using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public record RemoveAlarmFromAccountSensorCommand : IRequest
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
    public required Guid AlarmUid { get; init; }
}

public class RemoveAlarmFromAccountSensorCommandHandler : IRequestHandler<RemoveAlarmFromAccountSensorCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public RemoveAlarmFromAccountSensorCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(RemoveAlarmFromAccountSensorCommand request, CancellationToken cancellationToken)
    {
        var accountSensor =
            await _dbContext.Accounts
                .Where(a => a.Uid == request.AccountUid)
                .SelectMany(a => a.AccountSensors)
                .Include(@as => @as.Account)
                .Include(@as => @as.Sensor)
                .Include(@as => @as.Alarms)
                .SingleOrDefaultAsync(as2 => as2.Sensor.Uid == request.SensorUid, cancellationToken)
            ?? throw new AccountSensorNotFoundException("The account or sensor cannot be found.")
                { AccountUid = request.AccountUid, SensorUid = request.SensorUid };

        accountSensor.EnsureEnabled();

        var alarm = accountSensor.Alarms.Where(a => a.Uid == request.AlarmUid).FirstOrDefault()
            ?? throw new AccountSensorAlarmNotFoundException("The alarm cannot be found.")
                { AccountUid = request.AccountUid, SensorUid = request.SensorUid, AlarmUid = request.AlarmUid };
        accountSensor.RemoveAlarm(alarm);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}