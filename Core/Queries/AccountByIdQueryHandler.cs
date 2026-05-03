using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public record AccountByIdQuery : IRequest<Account?>
{
    public required int Id { get; init; }
}

public class AccountByIdQueryHandler : IRequestHandler<AccountByIdQuery, Account?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AccountByIdQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Account?> Handle(AccountByIdQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts
            .Where(a => a.Id == request.Id)
            .SingleOrDefaultAsync(cancellationToken);
    }
}
