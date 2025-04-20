using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Commands;

public record AddAccountSensorAlarmCommand : IRequest
{
    public required Guid AccountId { get; init; }
    public required Guid SensorId { get; init; }
    public required Guid AlarmId { get; init; }
    public required AccountSensorAlarmType AlarmType { get; init; }
    public double? AlarmThreshold { get; init; }

}

public class AddAccountSensorAlarmCommandHandler : IRequestHandler<AddAccountSensorAlarmCommand>
{
    private readonly WaterAlarmDbContext _dbContext;
    private readonly ILogger<AddAccountSensorAlarmCommandHandler> _logger;

    public AddAccountSensorAlarmCommandHandler(WaterAlarmDbContext dbContext, ILogger<AddAccountSensorAlarmCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(AddAccountSensorAlarmCommand request, CancellationToken cancellationToken)
    {
        var accountSensor =
            await _dbContext.Accounts
                .Where(a => a.Uid == request.AccountId)
                .SelectMany(a => a.AccountSensors)
                .Include(@as => @as.Account)
                .Include(@as => @as.Sensor)
                .Include(@as => @as.Alarms)
                .SingleOrDefaultAsync(as2 => as2.Sensor.Uid == request.SensorId, cancellationToken)
            ?? throw new AccountSensorNotFoundException("The account or sensor cannot be found.")
                { AccountUid = request.AccountId, SensorUid = request.SensorId };

        accountSensor.EnsureEnabled();

        accountSensor.AddAlarm(new AccountSensorAlarm
        {
            Uid = request.AlarmId,
            AlarmType = request.AlarmType,
            AlarmThreshold = request.AlarmThreshold
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}