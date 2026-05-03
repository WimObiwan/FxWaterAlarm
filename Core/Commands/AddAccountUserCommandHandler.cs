using Core.Entities;
using Core.Repositories;
using MediatR;

namespace Core.Commands;

public record AddAccountUserCommand : IRequest
{
    public required int AccountId { get; init; }
    public required AccountUserLoginType LoginType { get; init; }
    public string? Email { get; init; }
    public string? Provider { get; init; }
    public string? ProviderSubjectId { get; init; }
}

public class AddAccountUserCommandHandler : IRequestHandler<AddAccountUserCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public AddAccountUserCommandHandler(WaterAlarmDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(AddAccountUserCommand request, CancellationToken cancellationToken)
    {
        var accountUser = new AccountUser
        {
            AccountId = request.AccountId,
            LoginType = request.LoginType,
            Email = request.Email,
            Provider = request.Provider,
            ProviderSubjectId = request.ProviderSubjectId,
            CreationTimestamp = DateTime.UtcNow
        };
        _dbContext.AccountUsers.Add(accountUser);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
