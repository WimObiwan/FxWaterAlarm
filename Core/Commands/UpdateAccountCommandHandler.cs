using Core.Exceptions;
using Core.Repositories;
using Core.Util;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public record UpdateAccountCommand : IRequest
{
    public required Guid Uid { get; init; }
    public Optional<string> Email { get; init; }
    public Optional<string> Name { get; init; }
}

public class UpdateAccountCommandHandler : IRequestHandler<UpdateAccountCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public UpdateAccountCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(UpdateAccountCommand request, CancellationToken cancellationToken)
    {
        var account =
            await _dbContext.Accounts.SingleOrDefaultAsync(a => a.Uid == request.Uid, cancellationToken)
            ?? throw new AccountNotFoundException("The account cannot be found.") { Uid = request.Uid };

        if (request.Email is { Specified: true })
            account.Email = request.Email.Value ?? throw new ArgumentNullException(nameof(account.Email));
        if (request.Name is { Specified: true })
            account.Name = request.Name.Value;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}