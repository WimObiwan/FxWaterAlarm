using Core.Audit;
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
    private readonly IAuditService _auditService;

    public Core.Entities.Account? AccountEntity { get; set; }
    public IList<Tuple<Core.Entities.AccountSensor, IMeasurementEx?>>? AccountSensors { get; set; }
    public string? Message { get; set; }
    public bool CanUpdate { get; set; }

    public Account(IMediator mediator, IUserInfo userInfo, IAuditService auditService)
    {
        _mediator = mediator;
        _userInfo = userInfo;
        _auditService = auditService;
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
            CanUpdate = await _userInfo.CanUpdateAccount(AccountEntity);
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
        using var actionScope = _auditService.BeginAction("Account.AddSensor", new AuditTarget
        {
            AccountLink = accountLink,
            SensorLink = deveui
        });
        await _auditService.LogAsync(AuditOutcome.Attempted);

        try
        {
            string? message = null;

            var accountEntity = await _mediator.Send(new AccountByLinkQuery
            {
                Link = accountLink
            });

            if (accountEntity == null)
            {
                await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Account not found" });
                return NotFound();
            }

            if (!await _userInfo.CanUpdateAccount(accountEntity))
            {
                await _auditService.LogAsync(AuditOutcome.Denied, new AuditDetails { Reason = "Not authorized to update account" },
                    target: new AuditTarget { AccountUid = accountEntity.Uid, AccountLink = accountLink });
                return Forbid();
            }

            var sensorEntity = await _mediator.Send(new SensorByLinkQuery
            {
                SensorLink = deveui
            });

            if (sensorEntity == null)
            {
                message = "Sensor not found";
                await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails { Reason = "Sensor not found" },
                    target: new AuditTarget { AccountUid = accountEntity.Uid, AccountLink = accountLink, SensorLink = deveui });
            }
            else
            {
                await _mediator.Send(new AddSensorToAccountCommand
                {
                    AccountUid = accountEntity.Uid,
                    SensorUid = sensorEntity.Uid
                });

                message = "Sensor added successfully";
                await _auditService.LogAsync(AuditOutcome.Succeeded, target: new AuditTarget
                {
                    AccountUid = accountEntity.Uid,
                    AccountLink = accountLink,
                    SensorUid = sensorEntity.Uid,
                    DevEui = sensorEntity.DevEui
                });
            }

            return RedirectToPage(new { accountLink, message });
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails
            {
                Reason = "Unexpected exception",
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            });
            throw;
        }
    }
}