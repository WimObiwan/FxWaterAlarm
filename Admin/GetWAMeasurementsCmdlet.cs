using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using Core.Exceptions;
using Core.Queries;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Get, "WAMeasurements")]
[OutputType(typeof(Guid))]
public class GetWAMeasurementsCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ParameterSetName = "SensorId")]
    public Guid[] SensorId { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ParameterSetName = "Sensor")]
    public Sensor[] Sensor { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ParameterSetName = "AccountSensor")]
    public AccountSensor[] AccountSensor { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ParameterSetName = "DevEui")]
    public string[] DevEui { get; set; } = null!;

    [Parameter(
        Position = 1)]
    public DateTime? From { get; set; }

    [Parameter(
        Position = 2)]
    public DateTime? Till { get; set; }

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (ParameterSetName == "SensorId")
        {
            foreach (var sensorId in SensorId)
            {
                var sensor = await _mediator.Send(new SensorQuery() { Uid = sensorId }, cancellationToken);
                if (sensor != null)
                    await ProcessSingleAsync(sensor.DevEui);
                else
                {
                    Exception x = new SensorNotFoundException("The sensor cannot be found.") { SensorUid = sensorId };
                    WriteError(new ErrorRecord(x, x.GetType().Name, ErrorCategory.InvalidOperation, sensorId));
                }
            }
        }
        else if (ParameterSetName == "Sensor")
        {
            foreach (var sensor in Sensor)
                await ProcessSingleAsync(sensor.DevEui);
        }
        else if (ParameterSetName == "AccountSensor")
        {
            foreach (var accountSensor in AccountSensor)
                await ProcessSingleAsync(accountSensor.Sensor.DevEui);
        }
        else if (ParameterSetName == "DevEui")
        {
            foreach (var devEui in DevEui)
                await ProcessSingleAsync(devEui);
        }
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessSingleAsync(string devEui)
    {
        var results = await _mediator.Send(new MeasurementsQuery() { 
            DevEui = devEui,
            From = From,
            Till = Till
        });

        foreach (var result in results)
        {
            Return(result);
        }
    }

    private void Return(Core.Entities.Measurement measurement)
    {
        WriteObject(GetMeasurement(measurement));
    }

    public static Measurement GetMeasurement(Core.Entities.Measurement measurement)
    {
        return new Measurement {
            DevEui = measurement.DevEui,
            Timestamp = measurement.Timestamp,
            DistanceMm = measurement.DistanceMm,
            RssiDbm = measurement.RssiDbm,
            BatV = measurement.BatV
        };
    }

}

public class Measurement
{
    public required string DevEui { get; init; }
    public required DateTime Timestamp { get; init; }
    public required int DistanceMm { get; init; }
    public required double RssiDbm { get; init; }
    public required double BatV { get; init; }
}
