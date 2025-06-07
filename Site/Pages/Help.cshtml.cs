using Core.Communication;
using Core.Helpers;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class Help : PageModel
{
    private readonly IMediator _mediator;
    private readonly IUrlBuilder _urlBuilder;
    private readonly IMessenger _messenger;

    public string? Id { get; set; } = null;

    public Help(IMediator mediator, IUrlBuilder urlBuilder, IMessenger messenger)
    {
        _mediator = mediator;
        _urlBuilder = urlBuilder;
        _messenger = messenger;
    }
    
    public IActionResult OnGet()
    {
        return Page();
    }

    private async Task<Core.Entities.Account?> GetAccountByEmail(string email)
    {
        return await _mediator.Send(new AccountByEmailQuery() { Email = email });
    } 

    private async Task<Core.Entities.Sensor?> GetSensorById(string id)
    {
        return await _mediator.Send(new SensorByLinkQuery() { SensorLink = id, IncludeAccount = true });
    } 

    public async Task<IActionResult> OnPost(string id)
    {
        string? restPath = null;
        var account = await GetAccountByEmail(id);
        if (account == null)
        {
            var sensor = await GetSensorById(id);
            if (sensor == null)
            {
                // 5G sensors have an id starting with 'F', which is not printed on the sensor sticker.
                sensor = await GetSensorById('F' + id);
            }

            if (sensor != null)
            {
                var accountSensors = sensor.AccountSensors
                    .Where(@as => @as.Account.IsDemo == false);
                if (accountSensors.Count() == 1)
                {
                    var accountSensor = accountSensors.First();
                    restPath = accountSensor.RestPath;
                    account = accountSensor.Account;
                }
            }
        }

        if (account != null)
        {
            restPath ??= account.RestPath;

            if (restPath != null)
            {
                await _messenger.SendLinkMailAsync(
                        account.Email,
                        _urlBuilder.BuildUrl(restPath));
            }
        }

        return Redirect("?");
    }
}