using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Reset, "WASensorLink")]
public class NewWASensorLinkCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ParameterSetName = "SensorId")]
    public Guid SensorId { get; set; }

    [Parameter(
        Position = 0,
        Mandatory = true,
        ParameterSetName = "Sensor")]
    public Account Sensor { get; set; } = null!;

    [Parameter(
        Position = 1)]
    public string? Link { get; set; }

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (ParameterSetName == "SensorId")
        {
            await ProcessSingleAsync(SensorId);
        }
        else if (ParameterSetName == "Sensor")
        {
            await ProcessSingleAsync(Sensor.Id);
        }
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessSingleAsync(Guid sensorId)
    {
        await _mediator.Send(new RegenerateSensorLinkCommand() { 
            SensorUid = sensorId,
            Link = Link
        });
    }
}
