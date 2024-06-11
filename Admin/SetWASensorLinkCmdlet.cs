using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Reset,"WASensorLink")]
public class NewWASensorLinkCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true)]
    public Guid SensorUid { get; set; }

    [Parameter(
        Position = 1)]
    public string? Link { get; set; }

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        await _mediator.Send(new RegenerateSensorLinkCommand() { 
            SensorUid = SensorUid,
            Link = Link
        });
    }
}
