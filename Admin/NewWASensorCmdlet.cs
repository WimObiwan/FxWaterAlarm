using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using Core.Entities;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.New, "WASensor")]
[OutputType(typeof(Guid))]
public class NewWASensorCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true)]
    public string DevEui { get; set; } = null!;

    [Parameter(
        Position = 1,
        Mandatory = true)]
    public Core.Entities.SensorType SensorType { get; set; }

    [Parameter(
        Position = 2)]
    public Guid? SensorUid { get; set; }

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        Guid sensorUid = SensorUid ?? Guid.NewGuid();

        await _mediator.Send(new CreateSensorCommand() { 
            Uid = sensorUid,
            DevEui = DevEui,
            SensorType = SensorType
        });

        WriteObject(sensorUid);
    }
}
