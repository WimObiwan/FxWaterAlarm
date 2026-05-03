using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public record RemoveAccountUserCommand : IRequest
{
    public required int AccountUserId { get; init; }
}

public class RemoveAccountUserCommandHandler : IRequestHandler<RemoveAccountUserCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public RemoveAccountUserCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(RemoveAccountUserCommand request, CancellationToken cancellationToken)
    {
        var accountUser = await _dbContext.AccountUsers
            .SingleOrDefaultAsync(u => u.Id == request.AccountUserId, cancellationToken);

        if (accountUser == null)
            return;

        _dbContext.AccountUsers.Remove(accountUser);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}