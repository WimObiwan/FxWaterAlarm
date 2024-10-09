using System.Globalization;
using System.Text;
using Core.Commands;
using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Site.Utilities;

namespace Site.Pages;

public class Account : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUserInfo _userInfo;

    public Core.Entities.Account? AccountEntity { get; set; }
    public IList<MeasurementLevelEx>? Measurements { get; set; }

    public Account(IMediator mediator, IUserInfo userInfo)
    {
        _mediator = mediator;
        _userInfo = userInfo;
    }

    public async Task OnGet(string accountLink)
    {
        AccountEntity = await _mediator.Send(new AccountByLinkQuery
        {
            Link = accountLink
        });

        if (AccountEntity == null)
        {
            //...
        }
        else
        {
            Measurements = (await Task.WhenAll(AccountEntity.AccountSensors.Select(async accountSensor =>
            {
                var lastMeasurement = await _mediator.Send(new LastMeasurementLevelQuery
                    { DevEui = accountSensor.Sensor.DevEui });
                if (lastMeasurement != null)
                    return new MeasurementLevelEx(lastMeasurement, accountSensor);
                else
                    return null;
            })))
                .Where(m => m != null)
                .Cast<MeasurementLevelEx>()
                .ToList();
        }

    }
}