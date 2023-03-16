using Core.Exceptions;
using Core.Repositories;
using Core.Util;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public record UpdateAccountSensorCommand : IRequest
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
    public Optional<string> Name { get; init; }
    public Optional<int?> DistanceMmEmpty { get; init; }
    public Optional<int?> DistanceMmFull { get; init; }
    public Optional<int?> CapacityL { get; init; }
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
            ?? throw new AccountNotFoundException("The account or sensor cannot be found.")
                { Uid = request.AccountUid };

        if (request.Name is { Specified: true })
            account.Name = request.Name.Value;
        if (request.DistanceMmEmpty is { Specified: true })
            account.DistanceMmEmpty = request.DistanceMmEmpty.Value;
        if (request.DistanceMmFull is { Specified: true })
            account.DistanceMmFull = request.DistanceMmFull.Value;
        if (request.CapacityL is { Specified: true })
            account.CapacityL = request.CapacityL.Value;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}