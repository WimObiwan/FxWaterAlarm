using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountUserByProviderQuery : IRequest<AccountUser?>
{
    public required string Provider { get; init; }
    public required string ProviderSubjectId { get; init; }
}

public class AccountUserByProviderQueryHandler : IRequestHandler<AccountUserByProviderQuery, AccountUser?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountUserByProviderQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<AccountUser?> Handle(AccountUserByProviderQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.AccountUsers
            .Where(u => u.Provider == request.Provider && u.ProviderSubjectId == request.ProviderSubjectId)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
