using Core.Entities;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Queries;

public class ReadAccountByLinkQuery : IRequest<Account?>
{
    public required string Link { get; init; }
}

public class ReadAccountByLinkQueryHandler : IRequestHandler<ReadAccountByLinkQuery, Account?>
{
    private readonly WaterAlarmDbContext _dbContext;

    public ReadAccountByLinkQueryHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Account?> Handle(ReadAccountByLinkQuery request, CancellationToken cancellationToken)
    {
        return await _dbContext.Accounts.Where(a => a.Link == request.Link).SingleOrDefaultAsync(cancellationToken);
    }
}