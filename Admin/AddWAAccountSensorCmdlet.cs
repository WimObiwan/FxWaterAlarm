using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Add, "WAAccountSensor")]
[OutputType(typeof(Guid))]
public class AddWAAccountSensorCmdlet : DependencyCmdlet<Startup>
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

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        if (ParameterSetName == "AccountIdAndSensorId")
        {
            await ProcessSingleAsync(AccountId, SensorId);
        }
        else if (ParameterSetName == "AccountAndSensor")
        {
            await ProcessSingleAsync(Account.Id, Sensor.Id);
        }
        else
            throw new InvalidOperationException();
    }

    private async Task ProcessSingleAsync(Guid accountId, Guid sensorId)
    {
        await _mediator.Send(new AddSensorToAccountCommand() { 
            AccountUid = accountId,
            SensorUid = sensorId
        });
    }
}
