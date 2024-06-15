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
        var accountSensor = await _mediator.Send(new AccountSensorByLinkQuery
        {
            SensorLink = sensorLink
        });

        string? url = accountSensor?.RestPath;

        if (url == null)
            //...
            return NotFound();
        
        return Redirect(url);
    }
}