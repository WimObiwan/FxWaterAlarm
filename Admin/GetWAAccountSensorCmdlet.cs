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

[Cmdlet(VerbsCommon.Get,"WAAccountSensor")]
[OutputType(typeof(AccountSensor))]
public class GetWAAccountSensorCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true, 
        ParameterSetName = "AccountId")]
    public Guid[] AccountUid { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true, 
        ParameterSetName = "Account")]
    public Account[] Account { get; set; } = null!;

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (ParameterSetName == "AccountId")
        {
            foreach (var accountUid in AccountUid)
                await ProcessSingle(accountUid);
        }
        else if (ParameterSetName == "Account")
        {
            foreach (var account in Account)
                await ProcessSingle(account.Uid);
        }
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessSingle(Guid accountUid)
    {

        var accountSensors = await _mediator.Send(new AccountSensorsQuery() { AccountUid = accountUid });

        foreach (var accountSensor in accountSensors)
            Return(accountSensor);
    }

    private void Return(Core.Entities.AccountSensor accountSensor)
    {
        WriteObject(new AccountSensor {
            AccountUid = accountSensor.Account.Uid,
            SensorUid = accountSensor.Sensor.Uid,
            Name = accountSensor.Name,
            DistanceMmEmpty = accountSensor.DistanceMmEmpty,
            DistanceMmFull = accountSensor.DistanceMmFull,
            CapacityL = accountSensor.CapacityL,
            ResolutionL = accountSensor.ResolutionL,
            AlertsEnabled = accountSensor.AlertsEnabled,
            RestPath = accountSensor.RestPath,
            //AlarmsCount = accountSensor.Alarms.Count,
            Account = GetWAAccountCmdlet.GetAccount(accountSensor.Account),
            Sensor = GetWASensorCmdlet.GetSensor(accountSensor.Sensor),
            //Alarms = accountSensor.Alarms
        });
    }
}

public class AccountSensor
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
    public string? Name { get; init; }
    public int? DistanceMmEmpty { get; init; }
    public int? DistanceMmFull { get; init; }
    public int? CapacityL { get; init; }
    public double? ResolutionL { get; init; }
    public bool AlertsEnabled { get; init; }
    public string? RestPath { get; init; }
    //public int AlarmsCount { get; init; }
    required public Account Account { get; init; }
    required public Sensor Sensor { get; init; }
    //public IReadOnlyCollection<Core.Entities.AccountSensorAlarm> Alarms { get; init; } = null!;
    
}
