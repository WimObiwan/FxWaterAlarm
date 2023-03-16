using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record SensorsQuery : IRequest<List<Sensor>>
{
}

public class SensorsQueryHandler : IRequestHandler<SensorsQuery, List<Sensor>>
{
    private readonly WaterAlarmDbContext _dbContext;

    public SensorsQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<Sensor>> Handle(SensorsQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Sensors.ToListAsync(cancellationToken);
    }
}