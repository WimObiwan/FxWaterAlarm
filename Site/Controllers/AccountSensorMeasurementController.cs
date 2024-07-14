using Core.Queries;
using Core.Util;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Site.Controllers;

public class MeasurementResult
{
    public required DateTime TimeStamp { get; init; }
    public int? WaterL { get; init; }
}

[Route("api/a/{AccountLink}/s/{SensorLink}/m")]
public class AccountSensorMeasurementController : Controller
{
    private readonly IMediator _mediator;

    public async Task<IActionResult> Index(string accountLink, string sensorLink, [FromQuery]int fromDays = 7)
    {
        if (fromDays > 365) fromDays = 365;

        var accountSensor = await _mediator.Send(new AccountSensorByLinkQuery()
        {
            AccountLink = accountLink,
            SensorLink = sensorLink
        });

        if (accountSensor == null)
            return NotFound();

        DateTime from = DateTime.UtcNow.AddDays(-fromDays);
        DateTime? last = null;
        IEnumerable<MeasurementResult>? result = null;
        for (int i = 0; i < 3; i++)
        {
            var measurements = await _mediator.Send(new MeasurementsQuery
                {
                    DevEui = accountSensor.Sensor.DevEui,
                    From = from,
                    Till = last
                });

            if (measurements.Length == 0)
                break;

            last = measurements[^1].Timestamp;

            var result2 = measurements.Select(measurement =>
            {
                MeasurementEx measurementEx = new(measurement, accountSensor);
                int? waterL2;
                if (measurementEx.Distance.WaterL is {} waterL)
                    waterL2 = (int)waterL;
                else
                    waterL2 = null;
                return new MeasurementResult
                {
                    TimeStamp = measurement.Timestamp,
                    WaterL = waterL2
                };
            });

            if (result == null)
                result = result2;
            else
                result = result.Concat(result2);
        }

        return Ok(result?.Reverse());
    }

    public AccountSensorMeasurementController(IMediator mediator)
    {
        _mediator = mediator;
    }
}