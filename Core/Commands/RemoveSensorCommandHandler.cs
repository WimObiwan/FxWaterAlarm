using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public record RemoveSensorCommand : IRequest
{
    public required Guid SensorUid { get; init; }
}

public class RemoveSensorCommandHandler : IRequestHandler<RemoveSensorCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public RemoveSensorCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(RemoveSensorCommand request, CancellationToken cancellationToken)
    {
        var sensor =
            await _dbContext.Sensors
                .Include(s => s.AccountSensors)
                .SingleOrDefaultAsync(s => s.Uid == request.SensorUid);

        if (sensor == null)
            throw new SensorNotFoundException("The sensor cannot be found.") { SensorUid = request.SensorUid };

        if (sensor.AccountSensors.Any())
            throw new SensorCouldNotBeRemovedException("The sensor cannot be removed because it is still assigned to accounts.");

        _dbContext.Sensors.Remove(sensor);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}