using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public class ReadSensorByLinkQuery : IRequest<Sensor?>
{
    public required string SensorLink { get; init; }
    public string? AccountLink { get; init; }
}

public class ReadSensorByLinkQueryHandler : IRequestHandler<ReadSensorByLinkQuery, Sensor?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public ReadSensorByLinkQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Sensor?> Handle(ReadSensorByLinkQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Sensors
            .Where(s => 
                (s.Link == request.SensorLink || s.DevEui == request.SensorLink)
                && (request.AccountLink == null || s.Accounts.Any(a => a.Link == request.AccountLink)))
            .SingleOrDefaultAsync(cancellationToken);
    }
}