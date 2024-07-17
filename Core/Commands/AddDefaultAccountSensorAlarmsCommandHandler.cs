using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Commands;

public record AddDefaultAccountSensorAlarmsCommand : IRequest
{
    public required Guid AccountId { get; init; }
    public required Guid SensorId { get; init; }
}

public class AddDefaultAccountSensorAlarmsCommandHandler : AddDefaultAccountSensorAlarmsCommandHandlerBase, IRequestHandler<AddDefaultAccountSensorAlarmsCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AddDefaultAccountSensorAlarmsCommandHandler(WaterAlarmDbContext dbContext, ILogger<AddDefaultAccountSensorAlarmsCommandHandler> logger)
    : base (logger)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(AddDefaultAccountSensorAlarmsCommand request, CancellationToken cancellationToken)
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

        CreateAlarms(accountSensor);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}