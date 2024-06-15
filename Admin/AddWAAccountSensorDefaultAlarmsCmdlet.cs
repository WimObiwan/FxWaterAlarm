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

[Cmdlet(VerbsCommon.Add,"WAAccountSensorDefaultAlarms")]
[OutputType(typeof(AccountSensorAlarm))]
public class AddWAAccountSensorDefaultAlarmsCmdlet : DependencyCmdlet<Startup>
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
            await ProcessSingle(AccountId, SensorId);
        }
        else if (ParameterSetName == "AccountAndSensor")
        {
            await ProcessSingle(Account.Id, Sensor.Id);
        }
        else if (ParameterSetName == "AccountSensor")
        {
            foreach (var accountSensor in AccountSensor)
                await ProcessSingle(accountSensor.AccountId, accountSensor.SensorId);
        }
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessSingle(Guid accountUid, Guid sensorUid)
    {
        await _mediator.Send(new AddDefaultSensorAlarmsCommand()
        {
            AccountId = accountUid,
            SensorId = sensorUid
        });
    }
}
