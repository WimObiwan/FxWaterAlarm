using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public class AddSensorToAccountCommand : IRequest
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
}

public class AddSensorToAccountCommandHandler : IRequestHandler<AddSensorToAccountCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AddSensorToAccountCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(AddSensorToAccountCommand request, CancellationToken cancellationToken)
    {
        var account =
            await _dbContext.Accounts.SingleOrDefaultAsync(a => a.Uid == request.AccountUid, cancellationToken);
        if (account == null)
            throw new AccountNotFoundException("The account cannot be found.") { Uid = request.AccountUid };
        await _dbContext.Entry(account).Collection(a => a.AccountSensors).LoadAsync(cancellationToken);
        var sensor = await _dbContext.Sensors.SingleOrDefaultAsync(a => a.Uid == request.SensorUid, cancellationToken);
        if (sensor == null)
            throw new SensorNotFoundException("The sensor cannot be found.") { Uid = request.SensorUid };

        account.AddSensor(sensor);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}