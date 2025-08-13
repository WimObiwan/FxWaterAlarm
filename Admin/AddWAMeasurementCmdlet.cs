using System;
using System.Collections;
using System.Collections.Generic;
using System.Management.Automation;
using System.Threading;
using System.Threading.Tasks;
using Core.Commands;
using MediatR;
using Svrooij.PowerShell.DependencyInjection;

namespace WaterAlarmAdmin;

[Cmdlet(VerbsCommon.Add, "WAMeasurement")]
[OutputType(typeof(bool))]
public class AddWAMeasurementCmdlet : DependencyCmdlet<Startup>
{
    [ServiceDependency]
    internal IMediator _mediator { get; set; } = null!;

    [Parameter(
        Position = 0,
        Mandatory = true)]
    public string DevEui { get; set; } = null!;

    [Parameter(
        Position = 1)]
    public DateTime? Timestamp { get; set; }

    [Parameter(
        Position = 2,
        Mandatory = true)]
    public Hashtable Measurements { get; set; } = null!;

    public override async Task ProcessRecordAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Convert Hashtable to Dictionary<string, object>
            var measurementDict = new Dictionary<string, object>();
            foreach (DictionaryEntry entry in Measurements)
            {
                measurementDict[entry.Key.ToString() ?? string.Empty] = entry.Value ?? new object();
            }

            // Default to UTC now if no timestamp provided
            var timestamp = Timestamp ?? DateTime.UtcNow;

            await _mediator.Send(new AddMeasurementCommand
            {
                DevEui = DevEui,
                Timestamp = timestamp,
                Measurements = measurementDict
            }, cancellationToken);

            WriteObject(true);
        }
        catch (Exception ex)
        {
            WriteError(new ErrorRecord(ex, ex.GetType().Name, ErrorCategory.InvalidOperation, DevEui));
            WriteObject(false);
        }
    }
}