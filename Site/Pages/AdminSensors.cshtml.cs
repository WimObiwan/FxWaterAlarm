using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

[Authorize(Policy = "Admin")]
public class AdminSensors : PageModel
{
    private readonly IMediator _mediator;
    public IEnumerable<Core.Entities.Account> Accounts { get; set; } = null!;

    public AdminSensors(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task OnGet()
    {
        Accounts = await _mediator.Send(new AccountsQuery() { IncludeAccountSensors = true });
    }
}