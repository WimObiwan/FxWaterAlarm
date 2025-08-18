using Core.Exceptions;
using Core.Repositories;
using Core.Util;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public record UpdateSensorCommand : IRequest
{
    public required Guid Uid { get; init; }
    public Optional<int> ExpectedIntervalSecs { get; init; }
}

public class UpdateSensorCommandHandler : IRequestHandler<UpdateSensorCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public UpdateSensorCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateSensorCommand request, CancellationToken cancellationToken)
    {
        var sensor =
            await _dbContext.Sensors.SingleOrDefaultAsync(s => s.Uid == request.Uid, cancellationToken)
            ?? throw new SensorNotFoundException("The sensor cannot be found.") { SensorUid = request.Uid };

        if (request.ExpectedIntervalSecs is { Specified: true })
            sensor.ExpectedIntervalSecs = request.ExpectedIntervalSecs.Value;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}