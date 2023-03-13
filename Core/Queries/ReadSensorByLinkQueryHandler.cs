using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public class ReadSensorByLinkQuery : IRequest<AccountSensor?>
{
    public required string SensorLink { get; init; }
    public string? AccountLink { get; init; }
}

public class ReadSensorByLinkQueryHandler : IRequestHandler<ReadSensorByLinkQuery, AccountSensor?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public ReadSensorByLinkQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountSensor?> Handle(ReadSensorByLinkQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.Sensors
            .Where(s => s.Link == request.SensorLink || s.DevEui == request.SensorLink)
            .SelectMany(s => s.AccountSensors);

        if (request.AccountLink != null)
            query = query.Where(as2 => as2.Account.Link == request.AccountLink);

        return await query
            .Include(as2 => as2.Account)
            .Include(as2 => as2.Sensor)
            .SingleOrDefaultAsync(cancellationToken);
    }
}