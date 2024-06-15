using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record SensorByLinkQuery : IRequest<Sensor?>
{
    public required string SensorLink { get; init; }
}

public class SensorByLinkQueryHandler : IRequestHandler<SensorByLinkQuery, Sensor?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public SensorByLinkQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Sensor?> Handle(SensorByLinkQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Sensors
            .Where(s => s.Link == request.SensorLink || s.DevEui == request.SensorLink)
            .SingleOrDefaultAsync(cancellationToken);
    }
}