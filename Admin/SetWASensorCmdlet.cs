using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using Core.Util;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Set, "WASensor")]
public class SetWASensorCmdlet : DependencyCmdlet<Startup>
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
    public Sensor Sensor { get; set; } = null!;

    [Parameter]
    public int? ExpectedIntervalSecs { get; set; }

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        Guid sensorId;

        if (ParameterSetName == "SensorId")
        {
            sensorId = SensorId;
        }
        else if (ParameterSetName == "Sensor")
        {
            sensorId = Sensor.SensorId;
        }
        else
            throw new InvalidOperationException();

        await _mediator.Send(new UpdateSensorCommand() 
        { 
            Uid = sensorId,
            ExpectedIntervalSecs = Optional.From(ExpectedIntervalSecs)
        }, cancellationToken);
    }
}