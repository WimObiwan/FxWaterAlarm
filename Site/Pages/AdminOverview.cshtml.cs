using System.Globalization;
using System.Text;
using Core.Commands;
using Core.Entities;
using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Site.Utilities;

namespace Site.Pages;

public class AdminOverview : PageModel
{
    private readonly IAuthorizationService _authorizationService;

    public bool IsAdmin { get; set; }

    public AdminOverview(IAuthorizationService authorizationService)
    {
        _authorizationService = authorizationService;
    }

    public void OnGet()
    {
    }
}