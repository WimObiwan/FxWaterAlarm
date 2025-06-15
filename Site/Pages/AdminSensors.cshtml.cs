using Core.Commands;
using Core.Entities;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

[Authorize(Policy = "Admin")]
public class AdminSensors : PageModel
{
    private readonly IMediator _mediator;

    public string? Message { get; set; }
    public Core.Entities.Sensor? SensorEntity { get; set; }

    public AdminSensors(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGet(string? message, Guid? sensorUid)
    {
        Message = message;

        if (sensorUid.HasValue)
        {
            // Fetch the specific sensor if sensorUid is provided
            SensorEntity = await _mediator.Send(new SensorQuery() { Uid = sensorUid.Value });
        }
        else
        {
            SensorEntity = null;
        }
    }

    public async Task<IActionResult> OnPostAddSensor(string devEui, SensorType sensorType)
    {
        Guid sensorUid = Guid.NewGuid(); // Generate a new unique identifier for the account

        await _mediator.Send(new CreateSensorCommand()
        {
            Uid = sensorUid,
            DevEui = devEui,
            SensorType = sensorType
        });

        await _mediator.Send(new RegenerateSensorLinkCommand()
        {
            SensorUid = sensorUid
        });

        var sensor = await _mediator.Send(new SensorQuery()
        {
            Uid = sensorUid
        });

        if (sensor == null)
        {
            // Handle the case where the account could not be found
            return NotFound();
        }

        string message = "Sensor created successfully.";

        return RedirectToPage(new { message, sensorUid = sensor.Uid });
    }
}