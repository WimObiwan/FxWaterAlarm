using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsLifecycle.Invoke, "WACheckAllAccountSensorAlarms")]
public class InvokeWACheckAllAccountSensorAlarmsCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        await _mediator.Send(
            new CheckAllAccountSensorAlarmsCommand
            {
            });
    }
}
