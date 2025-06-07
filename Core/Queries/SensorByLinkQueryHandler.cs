using System.Diagnostics.Eventing.Reader;
using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record SensorByLinkQuery : IRequest<Sensor?>
{
    public required string SensorLink { get; init; }
    public bool IncludeAccount { get; init; } = false;
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
        IQueryable<Sensor> query =
            _dbContext.Sensors
                .Where(s => s.Link == request.SensorLink || s.DevEui == request.SensorLink);

        if (request.IncludeAccount)
        {
            query = query
                .Include(s => s.AccountSensors)
                .ThenInclude(@as => @as.Account);
        }

        return await query
            .SingleOrDefaultAsync(cancellationToken);
    }
}