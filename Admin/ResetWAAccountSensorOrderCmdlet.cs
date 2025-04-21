using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Reset, "WAAccountSensorOrder", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.Medium)]
[OutputType(typeof(AccountSensor))]
public class ResetWAAccountSensorOrderCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true, 
        ParameterSetName = "AccountId")]
    public Guid[] AccountId { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true, 
        ParameterSetName = "Account")]
    public Account[] Account { get; set; } = null!;

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (ParameterSetName == "AccountId")
        {
            foreach (var accountId in AccountId)
                await ProcessSingleAsync(accountId);
        }
        else if (ParameterSetName == "Account")
        {
            foreach (var account in Account)
                await ProcessSingleAsync(account.AccountId);
        }
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessSingleAsync(Guid accountId)
    {
        if (ShouldProcess(accountId.ToString(), $"Reset AccountSensor order of account {accountId}"))
        {
            await _mediator.Send(new ResetAccountSensorOrderCommand() { AccountUid = accountId });
        }
    }
}
