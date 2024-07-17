using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Core.Entities;
using Core.Exceptions;
using Core.Queries;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Get, "WAAccountSensorAlarm")]
[OutputType(typeof(AccountSensorAlarm))]
public class GetWAAccountSensorAlarmCmdlet : DependencyCmdlet<Startup>
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

    private async Task ProcessSingleAsync(Guid accountId, Guid sensorUid)
    {

        var accountSensorAlarms = await _mediator.Send(new AccountSensorAlarmsQuery()
        {
            AccountUid = accountId,
            SensorUid = sensorUid
        });

        foreach (var accountSensorAlarm in accountSensorAlarms)
            Return(accountId, sensorUid, accountSensorAlarm);
    }

    private void Return(Guid accountId, Guid sensorUid, Core.Entities.AccountSensorAlarm accountSensorAlarm)
    {
        WriteObject(new AccountSensorAlarm {
            AccountId = accountId,
            SensorId = sensorUid,
            AlarmUid = accountSensorAlarm.Uid,
            AlarmType = (AccountSensorAlarmType)(int)accountSensorAlarm.AlarmType,
            AlarmThreshold = accountSensorAlarm.AlarmThreshold,
        });
    }
}

public enum AccountSensorAlarmType
{
    Data = 1,
    Battery = 2,
    PercentageLow = 3,
    PercentageHigh = 4,
    //PercentageStatus = 5,
}

public class AccountSensorAlarm
{
    public required Guid AccountId { get; init; }
    public required Guid SensorId { get; init; }
    public required Guid AlarmUid { get; init; }
    public required AccountSensorAlarmType AlarmType { get; init; }
    public required double? AlarmThreshold { get; init; }
}
