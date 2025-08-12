using Core.Commands;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Site.Controllers;

[Route("api/s/{SensorLink}")]
public class SensorActionsController : Controller
{
    private readonly IMediator _mediator;

    public SensorActionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("check-alarms")]
    public async Task<IActionResult> CheckAlarms(string sensorLink, string? accountLink = null)
    {
        // Get the AccountSensor using the link (same pattern as existing controllers)
        var accountSensor = await _mediator.Send(new AccountSensorByLinkQuery()
        {
            SensorLink = sensorLink,
            AccountLink = accountLink
        });

        if (accountSensor == null)
            return NotFound();

        // Execute the same command that the PowerShell cmdlet uses
        await _mediator.Send(new CheckAccountSensorAlarmsCommand()
        {
            AccountUid = accountSensor.Account.Uid,
            SensorUid = accountSensor.Sensor.Uid
        });

        return Ok(new { message = "Alarm check completed successfully" });
    }
}