using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public record RemoveSensorFromAccountCommand : IRequest
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
}

public class RemoveSensorFromAccountCommandHandler : IRequestHandler<RemoveSensorFromAccountCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public RemoveSensorFromAccountCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(RemoveSensorFromAccountCommand request, CancellationToken cancellationToken)
    {
        var account =
            await _dbContext.Accounts.SingleOrDefaultAsync(a => a.Uid == request.AccountUid, cancellationToken)
            ?? throw new AccountNotFoundException("The account cannot be found.") { AccountUid = request.AccountUid };
        await _dbContext.Entry(account).Collection(a => a.AccountSensors).LoadAsync(cancellationToken);
        var sensor = await _dbContext.Sensors.SingleOrDefaultAsync(a => a.Uid == request.SensorUid, cancellationToken);
        if (sensor == null)
            throw new SensorNotFoundException("The sensor cannot be found.") { SensorUid = request.SensorUid };

        bool result = account.RemoveSensor(sensor);
        if (!result)
            throw new SensorCouldNotBeRemovedException("The sensor could not be removed.")
            {
                SensorUid = request.SensorUid, 
                AccountUid = request.AccountUid
            };

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}