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

    public async Task<IActionResult> OnPost(string id)
    {
        string? restPath = null;
        var account = await _mediator.Send(new AccountByEmailQuery() { Email = id });
        if (account == null)
        {
            var sensor = await _mediator.Send(new SensorByLinkQuery() { SensorLink = id, IncludeAccount = true });
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