using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record SensorQuery : IRequest<Sensor?>
{
    public required Guid Uid { get; init; }
}

public class SensorQueryHandler : IRequestHandler<SensorQuery, Sensor?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public SensorQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Sensor?> Handle(SensorQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Sensors
            .Where(s => s.Uid == request.Uid)
            .Include(a => a.AccountSensors)
            .ThenInclude(as2 => as2.Account)
            .SingleOrDefaultAsync(cancellationToken);
    }
}