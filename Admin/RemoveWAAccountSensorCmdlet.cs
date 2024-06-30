using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using MediatR;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Remove, "WAAccountSensor", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(Guid))]
public class RemoveWAAccountSensorCmdlet : DependencyCmdlet<Startup>
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
        ParameterSetName = "AccountSensor")]
    public AccountSensor AccountSensor { get; set; } = null!;

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
        if (ShouldProcess(sensorId.ToString(), $"Remove from account {accountId}"))
        {
            await _mediator.Send(new RemoveSensorFromAccountCommand() { 
                AccountUid = accountId,
                SensorUid = sensorId
            });
        }

    }
}
