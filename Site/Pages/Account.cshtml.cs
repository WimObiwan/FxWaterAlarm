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
    private readonly ILastMeasurementService _lastMeasurementService;

    public Core.Entities.Account? AccountEntity { get; set; }
    public IList<IMeasurementEx>? Measurements { get; set; }

    public Account(IMediator mediator, IUserInfo userInfo, ILastMeasurementService lastMeasurementService)
    {
        _mediator = mediator;
        _userInfo = userInfo;
        _lastMeasurementService = lastMeasurementService;
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
                return await _lastMeasurementService.GetLastMeasurement(accountSensor);
            })))
                .Where(m => m != null)
                .Select(m => m!)
                .ToList();
        }

    }
}