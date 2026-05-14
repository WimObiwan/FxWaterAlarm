using Core.Commands;
using Core.Entities;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

[Authorize(Policy = "Admin")]
public class AdminAccounts : PageModel
{
    private readonly IMediator _mediator;
    private readonly ILogger<AdminAccounts> _logger;
    public IEnumerable<Core.Entities.Account> Accounts { get; set; } = null!;

    public string? Message { get; set; }

    public AdminAccounts(IMediator mediator, ILogger<AdminAccounts> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task OnGet(string? message, Guid? sensorUid)
    {
        Message = message;

        // Fetch accounts with sensors included
        Accounts = await _mediator.Send(new AccountsQuery() { IncludeAccountSensors = true });
    }

    public async Task<IActionResult> OnPostAddAccount(string? name, string email)
    {
        if (string.IsNullOrWhiteSpace(name))
            name = null;

        Guid accountUid = Guid.NewGuid(); // Generate a new unique identifier for the account

        await _mediator.Send(new CreateAccountCommand()
        {
            Uid = accountUid,
            Name = name,
            Email = email
        });

        _logger.LogInformation(
            "Admin action: account created by {AdminEmail} from IP {IpAddress}; accountUid={AccountUid}, accountEmail={AccountEmail}, accountName={AccountName}",
            User.FindFirst("email")?.Value,
            HttpContext.Connection.RemoteIpAddress,
            accountUid,
            email,
            name);

        await _mediator.Send(new RegenerateAccountLinkCommand()
        {
            AccountUid = accountUid
        });

        await _mediator.Send(new RegenerateAccountLinkCommand()
        {
            AccountUid = accountUid
        });

        var account = await _mediator.Send(new AccountQuery()
        {
            Uid = accountUid
        });

        if (account == null)
        {
            // Handle the case where the account could not be found
            return NotFound();
        }

        string message = "Account created successfully.";

        return Redirect(account.RestPath + "?message=" + Uri.EscapeDataString(message));
    }

    public async Task<IActionResult> OnPostRemoveAccountSensor(Guid accountId, Guid sensorId)
    {
        // Remove the account sensor
        await _mediator.Send(new RemoveSensorFromAccountCommand()
        {
            AccountUid = accountId,
            SensorUid = sensorId
        });

        _logger.LogInformation(
            "Admin action: sensor removed from account by {AdminEmail} from IP {IpAddress}; accountUid={AccountUid}, sensorUid={SensorUid}",
            User.FindFirst("email")?.Value,
            HttpContext.Connection.RemoteIpAddress,
            accountId,
            sensorId);
 
        string message = "Account sensor removed successfully.";

        return RedirectToPage(new { message });
    }

    public IActionResult OnPostOpenAccount(string accountLink)
    {
        if (string.IsNullOrWhiteSpace(accountLink))
            return BadRequest();

        _logger.LogInformation(
            "Admin action: open account by {AdminEmail} from IP {IpAddress}; accountLink={AccountLink}",
            User.FindFirst("email")?.Value,
            HttpContext.Connection.RemoteIpAddress,
            accountLink);

        return Redirect($"/a/{accountLink}");
    }
}