using Core.Exceptions;
using Core.Repositories;
using Core.Util;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public record RegenerateAccountLinkCommand : IRequest
{
    public required Guid AccountUid { get; init; }
    public string? Link { get; init; }
}

public class RegenerateAccountLinkCommandHandler : IRequestHandler<RegenerateAccountLinkCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public RegenerateAccountLinkCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(RegenerateAccountLinkCommand request, CancellationToken cancellationToken)
    {
        var account =
            await _dbContext.Accounts.SingleOrDefaultAsync(a => a.Uid == request.AccountUid, cancellationToken)
            ?? throw new AccountNotFoundException("The account cannot be found.") { AccountUid = request.AccountUid };

        var link = request.Link ?? RandomLinkGenerator.Get();

        account.Link = link;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}