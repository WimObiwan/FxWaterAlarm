using Core.Communication;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Commands;

public record CheckAllAccountSensorAlarmsCommand : IRequest
{
}

public class CheckAllAccountSensorAlarmsCommandHandler : CheckAccountSensorAlarmsCommandHandlerBase, IRequestHandler<CheckAllAccountSensorAlarmsCommand>
{
    private readonly WaterAlarmDbContext _dbContext;
    private readonly ILogger<CheckAllAccountSensorAlarmsCommandHandler> _logger;

    public CheckAllAccountSensorAlarmsCommandHandler(WaterAlarmDbContext dbContext, IMediator mediator, IMessenger messenger, 
        ILogger<CheckAllAccountSensorAlarmsCommandHandler> logger)
        : base(dbContext, mediator, messenger, logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(CheckAllAccountSensorAlarmsCommand request, CancellationToken cancellationToken)
    {
        var accountSensors =
            _dbContext.Accounts
                .SelectMany(a => a.AccountSensors)
                .Include(@as => @as.Sensor)
                .Include(@as => @as.Account);
        
        await foreach (var accountSensor in accountSensors.AsAsyncEnumerable().WithCancellation(cancellationToken))
        {
            try
            {
                _logger.LogInformation("Handling {Account} {AccountSensorName} {sensor}", 
                    accountSensor.Account.Email, accountSensor.Name, accountSensor.Sensor.DevEui);
                
                await CheckAccountSensorAlarms(accountSensor, cancellationToken);
            }
            catch (Exception x)
            {
                _logger.LogError(x, "CheckAccountSensorAlarms failed for account {Account}", accountSensor.Account.Email);
            }
        }
    }
}
