using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Reset,"WAAccountLink")]
public class NewWAAccountLinkCmdlet : DependencyCmdlet<Startup>
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

    [Parameter(
        Position = 1)]
    public string? Link { get; set; }

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (ParameterSetName == "AccountId")
        {
            await ProcessSingleAsync(AccountId);
        }
        else if (ParameterSetName == "Account")
        {
            await ProcessSingleAsync(Account.Id);
        }
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessSingleAsync(Guid accountId)
    {
        await _mediator.Send(new RegenerateAccountLinkCommand() { 
            AccountUid = accountId,
            Link = Link
        });
    }
}
