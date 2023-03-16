using Core.Exceptions;
using Core.Repositories;
using Core.Util;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public record RegenerateSensorLinkCommand : IRequest
{
    public required Guid SensorUid { get; init; }
    public string? Link { get; init; }
}

public class RegenerateSensorLinkCommandHandler : IRequestHandler<RegenerateSensorLinkCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public RegenerateSensorLinkCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(RegenerateSensorLinkCommand request, CancellationToken cancellationToken)
    {
        var sensor =
            await _dbContext.Sensors.SingleOrDefaultAsync(a => a.Uid == request.SensorUid, cancellationToken)
            ?? throw new AccountNotFoundException("The account cannot be found.") { Uid = request.SensorUid };

        var link = request.Link ?? RandomLinkGenerator.Get();

        sensor.Link = link;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}