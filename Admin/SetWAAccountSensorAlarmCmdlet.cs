using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using Core.Entities;
using Core.Exceptions;
using Core.Queries;
using Core.Util;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Set, "WAAccountSensorAlarm")]
public class SetWAAccountSensorAlarmCmdlet : DependencyCmdlet<Startup>
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
    public AccountSensor AccountSensor { get; set; } = null!;

    [Parameter(
        Position = 2,
        Mandatory = true)]
    public Guid AlarmId { get; set; }

    [Parameter(
        Position = 3)]
    public AccountSensorAlarmType? AlarmType { get; set; }

    [Parameter(
        Position = 4)]
    public double? AlarmThreshold { get; set; }

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        Guid accountId, sensorId;

        if (ParameterSetName == "AccountIdAndSensorId")
        {
            accountId = AccountId;
            sensorId = SensorId;
        }
        else if (ParameterSetName == "AccountAndSensor")
        {
            accountId = Account.AccountId;
            sensorId = Sensor.SensorId;
        }
        else if (ParameterSetName == "AccountSensor")
        {
            accountId = AccountSensor.AccountId;
            sensorId = AccountSensor.SensorId;
        } 
        else
            throw new InvalidOperationException();

        await _mediator.Send(new UpdateAccountSensorAlarmCommand() 
        { 
            AccountUid = accountId,
            SensorUid = sensorId,
            AlarmUid = AlarmId,
            AlarmType = Optional.From((Core.Entities.AccountSensorAlarmType?)(int?)AlarmType),
            AlarmThreshold = Optional.From(AlarmThreshold, -1.0)
        });
    }
}
