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

[Cmdlet(VerbsCommon.Get, "WAAccount")]
[OutputType(typeof(Account))]
public class GetWAAccountCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        ValueFromPipeline = true,
        ParameterSetName = "AccountId")]
    public Guid[]? AccountId { get; set; }

    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true,
        ParameterSetName = "Email")]
    public string[] Email { get; set; } = null!;

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (ParameterSetName == "AccountId")
        {
            if (AccountId == null)
                await ProcessAll();
            else
                foreach (var accountId in AccountId)
                    await ProcessSingleAsync(accountId);
        }
        else if (ParameterSetName == "Email")
        {
            foreach (var email in Email)
                await ProcessSingle(email);
        }
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessAll()
    {
        var accounts = await _mediator.Send(new AccountsQuery());

        foreach (var account in accounts)
        {
            Return(account);
        }
    }

    private async Task ProcessSingleAsync(Guid accountId)
    {

        var account = await _mediator.Send(new AccountQuery() { Uid = accountId });
        if (account == null)
        {
            Exception x = new AccountNotFoundException("The account cannot be found.") { AccountUid = accountId };
            WriteError(new ErrorRecord(x, x.GetType().Name, ErrorCategory.InvalidOperation, accountId));
            return;
        }

        Return(account);
    }

    private async Task ProcessSingle(string email)
    {

        var account = await _mediator.Send(new AccountByEmailQuery() { Email = email });
        if (account == null)
        {
            Exception x = new AccountNotFoundException("The account cannot be found.") { Email = email };
            WriteError(new ErrorRecord(x, x.GetType().Name, ErrorCategory.InvalidOperation, Email));
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
            Id = account.Uid,
            Email = account.Email,
            Name = account.Name,
            CreationTimestamp = account.CreationTimestamp,
            Link = account.Link
        };
    }
}

public class Account
{
    public required Guid Id { get; init; }
    public required string Email { get; init; }
    public string? Name { get; init; }
    public required DateTime CreationTimestamp { get; init; }
    public string? Link { get; init; }
}
