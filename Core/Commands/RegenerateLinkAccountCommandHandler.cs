using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public class RegenerateLinkAccountCommand : IRequest
{
    public required Guid AccountUid { get; init; }
    public string? Link { get; init; }
}

public class RegenerateLinkAccountCommandHandler : IRequestHandler<RegenerateLinkAccountCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public RegenerateLinkAccountCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(RegenerateLinkAccountCommand request, CancellationToken cancellationToken)
    {
        var account =
            await _dbContext.Accounts.SingleOrDefaultAsync(a => a.Uid == request.AccountUid, cancellationToken)
            ?? throw new AccountNotFoundException("The account cannot be found.") { Uid = request.AccountUid };

        var link = request.Link ?? RandomString();

        account.Link = link;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public static string RandomString()
    {
        var rBytes = RandomNumberGenerator.GetBytes(8);
        var base64 = Convert.ToBase64String(rBytes);
        return Regex.Replace(base64, "[^A-Za-z0-9]", "");
    }
}