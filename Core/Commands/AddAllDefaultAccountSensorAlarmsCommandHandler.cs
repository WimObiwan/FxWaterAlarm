using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Commands;

public record AddAllDefaultAccountSensorAlarmsCommand : IRequest
{
}

public class AddAllDefaultAccountSensorAlarmsCommandHandler : AddDefaultAccountSensorAlarmsCommandHandlerBase, IRequestHandler<AddAllDefaultAccountSensorAlarmsCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AddAllDefaultAccountSensorAlarmsCommandHandler(WaterAlarmDbContext dbContext, ILogger<AddAllDefaultAccountSensorAlarmsCommandHandler> logger)
    : base(logger)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(AddAllDefaultAccountSensorAlarmsCommand request, CancellationToken cancellationToken)
    {
        var accountSensors =
            await _dbContext.Accounts
                .SelectMany(a => a.AccountSensors)
                .Include(@as => @as.Account)
                .Include(@as => @as.Sensor)
                .Include(@as => @as.Alarms)
                .ToListAsync(cancellationToken);

        foreach (var accountSensor in accountSensors)
            CreateAlarms(accountSensor);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}