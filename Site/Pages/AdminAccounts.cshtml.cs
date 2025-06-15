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
    public IEnumerable<Core.Entities.Account> Accounts { get; set; } = null!;

    public string? Message { get; set; }

    public AdminAccounts(IMediator mediator)
    {
        _mediator = mediator;
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
}