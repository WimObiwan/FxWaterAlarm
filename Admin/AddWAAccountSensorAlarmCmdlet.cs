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
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Add, "WAAccountSensorAlarm")]
[OutputType(typeof(AccountSensorAlarm))]
public class AddWAAccountSensorAlarmCmdlet : DependencyCmdlet<Startup>
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
    public AccountSensorAlarmType AlarmType { get; set; }

    [Parameter(
        Position = 3)]
    public double? AlarmThreshold { get; set; }

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
            await ProcessSingleAsync(AccountSensor.AccountId, AccountSensor.SensorId);
        }
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessSingleAsync(Guid accountId, Guid sensorId)
    {
        Guid alarmId = Guid.NewGuid();
        await _mediator.Send(new AddAccountSensorAlarmCommand()
        {
            AccountId = accountId,
            SensorId = sensorId,
            AlarmId = alarmId,
            AlarmType = (Core.Entities.AccountSensorAlarmType)(int)AlarmType,
            AlarmThreshold = AlarmThreshold
        });

        WriteObject(alarmId);
    }
}
