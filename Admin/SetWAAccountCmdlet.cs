using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using Core.Util;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Set, "WAAccount")]
public class SetWAAccountCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ParameterSetName = "AccountId")]
    public Guid AccountId { get; set; }

    [Parameter(
        Position = 0,
        Mandatory = true,
        ParameterSetName = "Account")]
    public Account Account { get; set; } = null!;

    [Parameter]
    public string? Name { get; set; }

    [Parameter]
    public string? Email { get; set; }

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        Guid accountId;

        if (ParameterSetName == "AccountId")
        {
            accountId = AccountId;
        }
        else if (ParameterSetName == "Account")
        {
            accountId = Account.AccountId;
        }
        else
            throw new InvalidOperationException();

        await _mediator.Send(new UpdateAccountCommand() 
        { 
            Uid = accountId,
            Name = Optional.From(Name),
            Email = Optional.From(Email)
        }, cancellationToken);
    }
}
