using System;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Threading;
using System.Threading.Tasks;
using Core.Exceptions;
using Core.Queries;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Get, "WAInfo")]
[OutputType(typeof(Info))]
public class GetWAInfoCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        ValueFromPipeline = true,
        ParameterSetName = "AccountId")]
    public Guid[]? AccountId { get; set; }

    [Parameter(
        Position = 0,
        Mandatory = true,
        ValueFromPipeline = true,
        ParameterSetName = "Email")]
    public string[] Email { get; set; } = null!;

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (ParameterSetName == "AccountId")
        {
            if (AccountId == null)
                await ProcessAll();
            else
                foreach (var accountId in AccountId)
                    await ProcessSingleAsync(accountId);
        }
        else if (ParameterSetName == "Email")
        {
            foreach (var email in Email)
                await ProcessSingleAsync(email);
        }
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessAll()
    {
        var accounts = await _mediator.Send(new AccountsQuery());

        foreach (var account in accounts)
        {
            await ProcessSingleAsync(account.Uid);
        }
    }

    private async Task ProcessSingleAsync(Guid accountId)
    {

        var accountSensors = await _mediator.Send(new AccountSensorsQuery() { AccountUid = accountId });

        foreach (var accountSensor in accountSensors)
        {
            Return(accountSensor);
        }
    }

    private async Task ProcessSingleAsync(string email)
    {
        var account = await _mediator.Send(new AccountByEmailQuery() { Email = email });
        if (account == null)
        {
            Exception x = new AccountNotFoundException("The account cannot be found.") { Email = email };
            WriteError(new ErrorRecord(x, x.GetType().Name, ErrorCategory.InvalidOperation, Email));
            return;
        }

        ProcessSingle(account);
    }

    private void ProcessSingle(Core.Entities.Account account)
    {
        foreach (var accountSensor in account.AccountSensors)
        {
            Return(accountSensor);
        }
    }

    public void Return(Core.Entities.AccountSensor accountSensor)
    {
        WriteObject(GetInfo(accountSensor));
    }

    public static Info GetInfo(Core.Entities.AccountSensor accountSensor)
    {
        return new Info {
            AccountId = accountSensor.Account.Uid,
            SensorId = accountSensor.Sensor.Uid,
            Email = accountSensor.Account.Email,
            DevEui = accountSensor.Sensor.DevEui,
            Name = accountSensor.Name,
            RestPath = accountSensor.RestPath
        };
    }
}

public class Info
{
    public required Guid AccountId { get; init; }
    public required Guid SensorId { get; init; }
    public required string Email { get; init; }
    public required string DevEui { get; init; }
    public string? Name { get; init; }
    public string? RestPath { get; init; }
}
