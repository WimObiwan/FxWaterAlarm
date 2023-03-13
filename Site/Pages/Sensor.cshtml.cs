using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class Sensor : PageModel
{
    private readonly IMediator _mediator;

    public Sensor(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task<IActionResult> OnGet(string sensorLink)
    {
        var accountSensor = await _mediator.Send(new ReadSensorByLinkQuery
        {
            SensorLink = sensorLink
        });

        if (accountSensor == null)
            //...
            return NotFound();
        return Redirect($"/a/{accountSensor.Account.Link}/s/{accountSensor.Sensor.Link}");
    }
}