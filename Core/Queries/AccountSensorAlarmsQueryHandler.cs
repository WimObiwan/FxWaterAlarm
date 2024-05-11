using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountSensorAlarmsQuery : IRequest<IEnumerable<AccountSensorAlarm>>
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
}

public class AccountSensorAlarmsQueryHandler : IRequestHandler<AccountSensorAlarmsQuery, IEnumerable<AccountSensorAlarm>>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountSensorAlarmsQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<AccountSensorAlarm>> Handle(AccountSensorAlarmsQuery request,
        CancellationToken cancellationToken)
    {
        var accountSensorAlarms =
            await _dbContext.Accounts
                .Where(a => a.Uid == request.AccountUid)
                .SelectMany(a => a.AccountSensors)
                .Where(@as => @as.Sensor.Uid == request.SensorUid)
                .SelectMany(@as => @as.Alarms)
                .ToListAsync(cancellationToken);

        return accountSensorAlarms;
    }
}