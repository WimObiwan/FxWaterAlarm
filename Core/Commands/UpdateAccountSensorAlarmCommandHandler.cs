using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using Core.Util;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public record UpdateAccountSensorAlarmCommand : IRequest
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
    public required Guid AlarmUid { get; init; }
    public required Optional<AccountSensorAlarmType> AlarmType { get; init; }
    public required Optional<double?> AlarmThreshold { get; init; }
}

public class UpdateAccountSensorAlarmCommandHandler : IRequestHandler<UpdateAccountSensorAlarmCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public UpdateAccountSensorAlarmCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateAccountSensorAlarmCommand request, CancellationToken cancellationToken)
    {
        var alarm =
            await _dbContext.Accounts
                .Where(a => a.Uid == request.AccountUid)
                .SelectMany(a => a.AccountSensors)
                .Where(@as => @as.Sensor.Uid == request.SensorUid)
                .SelectMany(@as => @as.Alarms)
                .Where(al => al.Uid == request.AlarmUid)
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AccountSensorAlarmNotFoundException("The account, sensor or alarm cannot be found.")
                { AccountUid = request.AccountUid, SensorUid = request.SensorUid, AlarmUid = request.AlarmUid };

        if (request.AlarmType.Specified)
            alarm.AlarmType = request.AlarmType.Value;

        if (request.AlarmThreshold.Specified)
            alarm.AlarmThreshold = request.AlarmThreshold.Value;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}