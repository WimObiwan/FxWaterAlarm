using Core.Commands;
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
    public IList<Tuple<Core.Entities.AccountSensor, IMeasurementEx?>>? AccountSensors { get; set; }
    public string? Message { get; set; }

    public Account(IMediator mediator, IUserInfo userInfo)
    {
        _mediator = mediator;
        _userInfo = userInfo;
    }

    public async Task OnGet(string accountLink, string? message = null)
    {
        Message = message;

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
            AccountSensors = (await Task.WhenAll(AccountEntity.AccountSensors.Select(async accountSensor =>
            {
                return Tuple.Create(
                    accountSensor,
                    await _mediator.Send(new LastMeasurementQuery
                    {
                        AccountSensor = accountSensor
                    }));
            })))
            .ToList();
        }
    }

    public async Task<IActionResult> OnPostAddSensorAsync(
        [FromRoute] string accountLink,
        [FromForm] string deveui)
    {
        string? message = null;

        var accountEntity = await _mediator.Send(new AccountByLinkQuery
        {
            Link = accountLink
        });

        if (accountEntity == null)
            return NotFound();

        if (!await _userInfo.CanUpdateAccount(accountEntity))
            return Forbid();

        var sensorEntity = await _mediator.Send(new SensorByLinkQuery
        {
            SensorLink = deveui
        });

        if (sensorEntity == null)
        {
            message = "Sensor not found";
        }
        else
        {
            await _mediator.Send(new AddSensorToAccountCommand
            {
                AccountUid = accountEntity.Uid,
                SensorUid = sensorEntity.Uid
            });
            
            message = "Sensor added successfully";
        }

        return RedirectToPage(new { accountLink, message });
    }
}