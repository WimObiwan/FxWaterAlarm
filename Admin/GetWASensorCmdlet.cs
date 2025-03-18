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

[Cmdlet(VerbsCommon.Get, "WASensor")]
[OutputType(typeof(Sensor))]
public class GetWASensorCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        ValueFromPipeline = true,
        ParameterSetName = "SensorId")]
    public Guid[]? SensorId { get; set; }

    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true,
        ParameterSetName = "DevEui")]
    public string[] DevEui { get; set; } = null!;

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (ParameterSetName == "SensorId")
        {
            if (SensorId == null)
                await ProcessAll();
            else
                foreach (var sensorUid in SensorId)
                    await ProcessSingle(sensorUid);
        }
        else if (ParameterSetName == "DevEui")
        {
            foreach (var devEui in DevEui)
                await ProcessSingle(devEui);
        }
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessAll()
    {
        var sensors = await _mediator.Send(new SensorsQuery());

        foreach (var sensor in sensors)
        {
            Return(sensor);
        }
    }

    private async Task ProcessSingle(Guid sensorId)
    {

        var sensor = await _mediator.Send(new SensorQuery() { Uid = sensorId });
        if (sensor == null)
        {
            Exception x = new SensorNotFoundException("The sensor cannot be found.") { SensorUid = sensorId };
            WriteError(new ErrorRecord(x, x.GetType().Name, ErrorCategory.InvalidOperation, sensorId));
            return;
        }

        Return(sensor);
    }

    private async Task ProcessSingle(string devEui)
    {

        var sensor = await _mediator.Send(new SensorByLinkQuery() { SensorLink = devEui });
        if (sensor == null)
        {
            Exception x = new SensorNotFoundException("The sensor cannot be found.") { DevEui = devEui };
            WriteError(new ErrorRecord(x, x.GetType().Name, ErrorCategory.InvalidOperation, devEui));
            return;
        }

        Return(sensor);
    }

    private void Return(Core.Entities.Sensor sensor)
    {
        WriteObject(GetSensor(sensor));
    }

    public static Sensor GetSensor(Core.Entities.Sensor sensor)
    {
        return new Sensor {
            SensorId = sensor.Uid,
            DevEui = sensor.DevEui,
            CreationTimestamp = sensor.CreateTimestamp,
            Link = sensor.Link,
            Type = (SensorType)(int)sensor.Type
        };
    }
}

public enum SensorType
{
    Level = 0, 
    Detect = 1,
    Moisture = 2
}

public class Sensor
{
    public required Guid SensorId { get; init; }
    public required string DevEui { get; init; }
    public required DateTime CreationTimestamp { get; init; }
    public string? Link { get; init; }
    public SensorType Type { get; init; }
}
