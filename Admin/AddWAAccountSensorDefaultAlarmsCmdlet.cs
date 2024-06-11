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

        await _mediator.Send(new AddDefaultSensorAlarmsCommand()
        {
            AccountUid = accountUid,
            SensorUid = sensorUid
        });
    }
}
