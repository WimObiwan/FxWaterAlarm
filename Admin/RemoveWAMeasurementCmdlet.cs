using System;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using Core.Queries;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Remove, "WAMeasurement", SupportsShouldProcess = true, ConfirmImpact = ConfirmImpact.High)]
[OutputType(typeof(bool))]
public class RemoveWAMeasurementCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true)]
    public string DevEui { get; set; } = null!;

    [Parameter(
        Position = 1,
        Mandatory = true)]
    public DateTime Timestamp { get; set; }

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Find the sensor by DevEUI to get its UID
            var sensor = await _mediator.Send(new SensorByLinkQuery { SensorLink = DevEui }, cancellationToken);
            
            if (sensor == null)
            {
                WriteError(new ErrorRecord(
                    new ArgumentException($"Sensor with DevEUI '{DevEui}' not found"),
                    "SensorNotFound",
                    ErrorCategory.ObjectNotFound,
                    DevEui));
                WriteObject(false);
                return;
            }

            if (ShouldProcess($"DevEUI: {DevEui}, Timestamp: {Timestamp:yyyy-MM-dd HH:mm:ss} UTC", 
                "Remove measurement"))
            {
                await _mediator.Send(new RemoveMeasurementCommand
                {
                    SensorUid = sensor.Uid,
                    Timestamp = Timestamp
                }, cancellationToken);

                WriteObject(true);
            }
            else
            {
                WriteObject(false);
            }
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.InvalidOperation, DevEui));
            WriteObject(false);
        }
    }
}