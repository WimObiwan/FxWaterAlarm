using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Commands;

public record CreateAccountCommand : IRequest
{
    public required Guid Uid { get; init; }
    public required string Email { get; init; }
    public string? Name { get; init; }
}

public class CreateAccountCommandHandler : IRequestHandler<CreateAccountCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public CreateAccountCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(CreateAccountCommand request, CancellationToken cancellationToken)
    {
        var account = new Account
        {
            Uid = request.Uid,
            Email = request.Email,
            Name = request.Name,
            CreationTimestamp = DateTime.UtcNow
        };
        _dbContext.Accounts.Add(account);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}