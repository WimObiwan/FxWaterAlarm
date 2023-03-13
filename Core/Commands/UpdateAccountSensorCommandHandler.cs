using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public class UpdateAccountSensorCommand : IRequest
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
    public Tuple<bool, int?>? DistanceMmEmpty { get; init; }
    public Tuple<bool, int?>? DistanceMmFull { get; init; }
    public Tuple<bool, int?>? CapacityL { get; init; }
}

public class UpdateAccountSensorCommandHandler : IRequestHandler<UpdateAccountSensorCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public UpdateAccountSensorCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateAccountSensorCommand request, CancellationToken cancellationToken)
    {
        var account =
            await _dbContext.Accounts
                .Where(a => a.Uid == request.AccountUid)
                .SelectMany(a => a.AccountSensors)
                .SingleOrDefaultAsync(as2 => as2.Sensor.Uid == request.SensorUid, cancellationToken)
            ?? throw new AccountNotFoundException("The account or sensor cannot be found.") { Uid = request.AccountUid };

        if (request.DistanceMmEmpty is { Item1: true })
            account.DistanceMmEmpty = request.DistanceMmEmpty.Item2;
        if (request.DistanceMmFull is { Item1: true })
            account.DistanceMmFull = request.DistanceMmFull.Item2;
        if (request.CapacityL is { Item1: true })
            account.CapacityL = request.CapacityL.Item2;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}