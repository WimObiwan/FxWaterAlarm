using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Commands;

public record CreateSensorCommand : IRequest
{
    public required Guid Uid { get; init; }
    public required string DevEui { get; init; }
}

public class CreateSensorCommandHandler : IRequestHandler<CreateSensorCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public CreateSensorCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(CreateSensorCommand request, CancellationToken cancellationToken)
    {
        var sensor = new Sensor
        {
            Uid = request.Uid,
            DevEui = request.DevEui,
            CreateTimestamp = DateTime.UtcNow
        };
        _dbContext.Sensors.Add(sensor);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}