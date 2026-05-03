using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountUsersByAccountQuery : IRequest<IReadOnlyList<AccountUser>>
{
    public required int AccountId { get; init; }
}

public class AccountUsersByAccountQueryHandler : IRequestHandler<AccountUsersByAccountQuery, IReadOnlyList<AccountUser>>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountUsersByAccountQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<AccountUser>> Handle(AccountUsersByAccountQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.AccountUsers
            .Where(u => u.AccountId == request.AccountId)
            .ToListAsync(cancellationToken);
    }
}
