using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Core.Commands;

public record UpdateAccountCommand : IRequest
{
    public required Guid Uid { get; init; }
    public Tuple<bool, string>? Email { get; init; }
    public Tuple<bool, string?>? Name { get; init; }
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

        if (request.Email is { Item1: true })
            account.Email = request.Email.Item2;
        if (request.Name is { Item1: true })
            account.Name = request.Name.Item2;

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}