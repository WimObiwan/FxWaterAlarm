using Core.Communication;
using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using Core.Util;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Commands;

public record ResetAccountSensorOrderCommand : IRequest
{
    public required Guid? AccountUid { get; init; }
}

public class ResetAccountSensorOrderCommandHandler : IRequestHandler<ResetAccountSensorOrderCommand>
{
    private readonly WaterAlarmDbContext _dbContext;
    private readonly ILogger<CheckAllAccountSensorAlarmsCommandHandler> _logger;

    public ResetAccountSensorOrderCommandHandler(WaterAlarmDbContext dbContext, 
        ILogger<CheckAllAccountSensorAlarmsCommandHandler> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Handle(ResetAccountSensorOrderCommand request, CancellationToken cancellationToken)
    {
        IQueryable<Account> accounts =
            _dbContext.Accounts
                .Include(a => a.AccountSensors);

        bool changed = false;

        if (request.AccountUid.HasValue)
        {
            var account = await accounts
                .Where(a => a.Uid == request.AccountUid.Value)
                .SingleOrDefaultAsync() 
                ?? throw new AccountNotFoundException("The account cannot be found.")
                { AccountUid = request.AccountUid.Value };

            changed = ResetAccountSensorOrderHelper.ResetOrder(_logger, account);
        }
        else
        {
            await foreach (var account in accounts.AsAsyncEnumerable().WithCancellation(cancellationToken))
            {
                try
                {
                    _logger.LogInformation("Handling {Account}", 
                        account.Email);
                    
                    if (ResetAccountSensorOrderHelper.ResetOrder(_logger, account))
                        changed = true;
                }
                catch (Exception x)
                {
                    _logger.LogError(x, "ResetOrder failed for account {Account}", account.Email);
                }
            }
        }

        if (changed)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Account sensor order reset successfully.");
        }
        else
        {
            _logger.LogInformation("No account sensor order reset needed.");
        }
    }

}
