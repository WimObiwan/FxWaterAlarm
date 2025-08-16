using System;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Queries;
using Core.Util;
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
        ParameterSetName = "AccountIdAndSensorId")]
    public Guid AccountId { get; set; }

    [Parameter(
        Position = 1,
        Mandatory = true,
        ParameterSetName = "AccountIdAndSensorId")]
    public Guid SensorId { get; set; }

    [Parameter(
        Position = 0,
        Mandatory = true,
        ParameterSetName = "AccountAndSensor")]
    public Account Account { get; set; } = null!;

    [Parameter(
        Position = 1,
        Mandatory = true,
        ParameterSetName = "AccountAndSensor")]
    public Sensor Sensor { get; set; } = null!;

    [Parameter(
        Position = 1,
        Mandatory = true,
        ValueFromPipeline = true,
        ParameterSetName = "AccountSensor")]
    public AccountSensor[] AccountSensor { get; set; } = null!;

    [Parameter(
        Position = 2)]
    public DateTime? From { get; set; }

    [Parameter(
        Position = 3)]
    public DateTime? Till { get; set; }

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (ParameterSetName == "AccountIdAndSensorId")
        {
            await ProcessSingleAsync(AccountId, SensorId);
        }
        else if (ParameterSetName == "AccountAndSensor")
        {
            await ProcessSingleAsync(Account.AccountId, Sensor.SensorId);
        }
        else if (ParameterSetName == "AccountSensor")
        {
            foreach (var accountSensor in AccountSensor)
                await ProcessSingleAsync(accountSensor.AccountId, accountSensor.SensorId);
        } 
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessSingleAsync(Guid accountId, Guid sensorId)
    {
        var accountSensorEntity = await _mediator.Send(new AccountSensorByIdQuery() { AccountUid = accountId, SensorUid = sensorId });

        if (accountSensorEntity != null)
        {
            var results = await _mediator.Send(new MeasurementsQuery() {
                AccountSensor = accountSensorEntity,
                From = From,
                Till = Till
            });

            if (results != null)
            {
                foreach (var measurement in results)
                    Return(measurement);
            }
        }
    }

    private void Return(IMeasurementEx measurement)
    {
        WriteObject(GetMeasurement(measurement));
    }

    public static Measurement GetMeasurement(IMeasurementEx measurement)
    {
        return new Measurement {
            DevEui = measurement.DevEui,
            Timestamp = measurement.Timestamp,
            RssiDbm = measurement.RssiDbm,
            BatV = measurement.BatV,
            Values = measurement.GetValues()
        };
    }
}

public class Measurement
{
    public required string DevEui { get; init; }
    public required DateTime Timestamp { get; init; }
    public required double RssiDbm { get; init; }
    public required double BatV { get; init; }
    public required IReadOnlyDictionary<string, object> Values { get; set; }
}
