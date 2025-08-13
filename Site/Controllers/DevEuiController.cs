using Core.Commands;
using MediatR;
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

    public DevEuiController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{devEui}")]
    public async Task<IActionResult> AddMeasurement(string devEui, [FromBody] AddMeasurementRequest request)
    {
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

            return Ok(new { success = true, message = "Measurement added successfully" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (NotSupportedException ex)
        {
            return BadRequest(new { success = false, error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { success = false, error = "An unexpected error occurred", details = ex.Message });
        }
    }
}