using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Queries;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Get,"WAAccount")]
[OutputType(typeof(Account))]
public class GetWAAccountCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        ValueFromPipeline = true)]
    public Guid[]? AccountUid { get; set; }

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (AccountUid == null)
            await ProcessAll();
        else
            foreach (var accountUid in AccountUid)
                await ProcessSingle(accountUid);
    }

    private async Task ProcessAll()
    {
        var accounts = await _mediator.Send(new AccountsQuery());

        foreach (var account in accounts)
        {
            Return(account);
        }
    }

    private async Task ProcessSingle(Guid accountUid)
    {

        var account = await _mediator.Send(new AccountQuery() { Uid = accountUid });
        if (account == null)
        {
            Exception x = new AccountNotFoundException("The account cannot be found.") { AccountUid = accountUid };
            WriteError(new ErrorRecord(x, x.GetType().Name, ErrorCategory.InvalidOperation, accountUid));
            return;
        }

        Return(account);
    }

    public void Return(Core.Entities.Account account)
    {
        WriteObject(GetAccount(account));
    }

    public static Account GetAccount(Core.Entities.Account account)
    {
        return new Account {
            Uid = account.Uid,
            Email = account.Email,
            Name = account.Name,
            CreationTimestamp = account.CreationTimestamp,
            Link = account.Link
        };
    }
}

public class Account
{
    public required Guid Uid { get; init; }
    public required string Email { get; init; }
    public string? Name { get; init; }
    public required DateTime CreationTimestamp { get; init; }
    public string? Link { get; init; }
}
