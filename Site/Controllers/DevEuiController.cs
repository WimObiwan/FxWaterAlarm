using Core.Audit;
using Core.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Site.Controllers;

public class AddMeasurementRequest
{
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Measurements { get; set; } = new();
}

[Route("api/deveui")]
public class DevEuiController : Controller
{
    private readonly IMediator _mediator;
    private readonly IAuditService _auditService;

    public DevEuiController(IMediator mediator, IAuditService auditService)
    {
        _mediator = mediator;
        _auditService = auditService;
    }

    [HttpPost("{devEui}/Measurements")]
    [Authorize(Policy = "ApiKey")]
    public async Task<IActionResult> AddMeasurement(string devEui, [FromBody] AddMeasurementRequest request)
    {
        using var actionScope = _auditService.BeginAction("Measurement.AddViaApiKey", new AuditTarget { DevEui = devEui });
        await _auditService.LogAsync(AuditOutcome.Attempted);

        try
        {
            // Default to UTC now if no timestamp provided
            var timestamp = request.Timestamp == default ? DateTime.UtcNow : request.Timestamp;

            await _mediator.Send(new AddMeasurementCommand
            {
                DevEui = devEui,
                Timestamp = timestamp,
                Measurements = request.Measurements
            });

            await _auditService.LogAsync(AuditOutcome.Succeeded, target: new AuditTarget { DevEui = devEui });

            return Ok(new { success = true, message = "Measurement added successfully" });
        }
        catch (InvalidOperationException ex)
        {
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails
            {
                Reason = "Invalid operation",
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            }, target: new AuditTarget { DevEui = devEui });
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails
            {
                Reason = "Invalid arguments",
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            }, target: new AuditTarget { DevEui = devEui });
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails
            {
                Reason = "Unsupported operation",
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            }, target: new AuditTarget { DevEui = devEui });
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            await _auditService.LogAsync(AuditOutcome.Failed, new AuditDetails
            {
                Reason = "Unexpected exception",
                ExceptionType = ex.GetType().Name,
                Message = ex.Message
            }, target: new AuditTarget { DevEui = devEui });
            return StatusCode(500, new { success = false, error = "An unexpected error occurred", details = ex.Message });
        }
    }
}