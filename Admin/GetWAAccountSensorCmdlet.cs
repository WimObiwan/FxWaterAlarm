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

[Cmdlet(VerbsCommon.Get, "WAAccountSensor")]
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
    public Guid[] AccountId { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true, 
        ParameterSetName = "Account")]
    public Account[] Account { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true,
        ParameterSetName = "All")]
    public SwitchParameter All { get; set; }

    [Parameter]
    public SwitchParameter IncludeDisabled { get; set; } = false;

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (ParameterSetName == "AccountId")
        {
            foreach (var accountId in AccountId)
                await ProcessSingleAsync(accountId);
        }
        else if (ParameterSetName == "Account")
        {
            foreach (var account in Account)
                await ProcessSingleAsync(account.AccountId);
        }
        else if (ParameterSetName == "All" && All.IsPresent)
        {
            await ProcessSingleAsync(null);
        }
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessSingleAsync(Guid? accountId)
    {
        var accountSensors = await _mediator.Send(new AccountSensorsQuery() { AccountUid = accountId, IncludeDisabled = IncludeDisabled });

        foreach (var accountSensor in accountSensors)
            Return(accountSensor);
    }

    private void Return(Core.Entities.AccountSensor accountSensor)
    {
        WriteObject(new AccountSensor {
            AccountId = accountSensor.Account.Uid,
            SensorId = accountSensor.Sensor.Uid,
            Disabled = accountSensor.Disabled,
            Order = accountSensor.Order,
            Name = accountSensor.Name,
            DistanceMmEmpty = accountSensor.DistanceMmEmpty,
            DistanceMmFull = accountSensor.DistanceMmFull,
            CapacityL = accountSensor.CapacityL,
            ResolutionL = accountSensor.ResolutionL,
            AlertsEnabled = accountSensor.AlertsEnabled,
            NoMinMaxConstraints = accountSensor.NoMinMaxConstraints,
            ManholeAreaM2 = accountSensor.ManholeAreaM2,
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
    public required Guid AccountId { get; init; }
    public required Guid SensorId { get; init; }
    public bool Disabled { get; init; }
    public int Order { get; init; }
    public string? Name { get; init; }
    public int? DistanceMmEmpty { get; init; }
    public int? DistanceMmFull { get; init; }
    public int? CapacityL { get; init; }
    public double? ResolutionL { get; init; }
    public bool AlertsEnabled { get; init; }
    public bool NoMinMaxConstraints { get; init; }
    public double? ManholeAreaM2 { get; init; }
    public string? RestPath { get; init; }
    //public int AlarmsCount { get; init; }
    required public Account Account { get; init; }
    required public Sensor Sensor { get; init; }
    //public IReadOnlyCollection<Core.Entities.AccountSensorAlarm> Alarms { get; init; } = null!;
    
}
