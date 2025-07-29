using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Remove, "WASensor", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
public class RemoveWASensorCmdlet : DependencyCmdlet<Startup>
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

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (ParameterSetName == "SensorId")
        {
            await ProcessSingleAsync(SensorId);
        }
        else if (ParameterSetName == "Sensor")
        {
            await ProcessSingleAsync(Sensor.SensorId);
        }
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessSingleAsync(Guid sensorId)
    {
        if (ShouldProcess(sensorId.ToString(), $"Remove sensor {sensorId}"))
        {
            await _mediator.Send(new RemoveSensorCommand() { 
                SensorUid = sensorId
            });
        }

    }
}
