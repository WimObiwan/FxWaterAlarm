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

[Cmdlet(VerbsCommon.Get,"WAAccountSensorAlarm")]
[OutputType(typeof(AccountSensorAlarm))]
public class GetWAAccountSensorAlarmCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ParameterSetName = "AccountIdAndSensorId")]
    public Guid AccountUid { get; set; }

    [Parameter(
        Position = 1,
        Mandatory = true,
        ParameterSetName = "AccountIdAndSensorId")]
    public Guid SensorUid { get; set; }

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
            await ProcessSingle(AccountUid, SensorUid);
        }
        else if (ParameterSetName == "AccountSensor")
        {
            foreach (var accountSensor in AccountSensor)
                await ProcessSingle(accountSensor.AccountUid, accountSensor.SensorUid);
        } 
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessSingle(Guid accountUid, Guid sensorUid)
    {

        var accountSensorAlarms = await _mediator.Send(new AccountSensorAlarmsQuery()
        {
            AccountUid = accountUid,
            SensorUid = sensorUid
        });

        foreach (var accountSensorAlarm in accountSensorAlarms)
            Return(accountUid, sensorUid, accountSensorAlarm);
    }

    private void Return(Guid accountUid, Guid sensorUid, Core.Entities.AccountSensorAlarm accountSensorAlarm)
    {
        WriteObject(new AccountSensorAlarm {
            AccountUid = accountUid,
            SensorUid = sensorUid,
            AlarmUid = accountSensorAlarm.Uid,
            AlarmType = (AccountSensorAlarmType)(int)accountSensorAlarm.AlarmType,
            AlarmThreshold = accountSensorAlarm.AlarmThreshold,
        });
    }
}

public enum AccountSensorAlarmType { Data = 1, Battery = 2, LevelFractionLow = 3, LevelFractionHigh = 4, LevelFractionStatus = 5 }

public class AccountSensorAlarm
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
    public required Guid AlarmUid { get; init; }
    public required AccountSensorAlarmType AlarmType { get; init; }
    public required double? AlarmThreshold { get; init; }
}
