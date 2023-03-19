using Core.Entities;
using Core.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Site.Pages;

public class MeasurementDistance
{
    private readonly Core.Entities.AccountSensor _accountSensor;

    public MeasurementDistance(int? distanceMm, Core.Entities.AccountSensor accountSensor)
    {
        DistanceMm = distanceMm;
        _accountSensor = accountSensor;
    }

    public int? DistanceMm { get; }

    public double? RealLevelFraction
    {
        get
        {
            if (DistanceMm.HasValue && _accountSensor is { DistanceMmEmpty: not null, DistanceMmFull: not null })
                return ((double)_accountSensor.DistanceMmEmpty.Value - DistanceMm.Value)
                       / ((double)_accountSensor.DistanceMmEmpty.Value - _accountSensor.DistanceMmFull.Value);
            return null;
        }
    }

    public double? LevelFraction
    {
        get
        {
            var realLevelFraction = RealLevelFraction;
            if (!realLevelFraction.HasValue)
                return null;
            if (realLevelFraction.Value > 1.0)
                return 1.0;
            if (realLevelFraction.Value < 0.0)
                return 0.0;
            return realLevelFraction;
        }
    }

    public double? WaterL
    {
        get
        {
            var levelFraction = LevelFraction;
            if (levelFraction != null && _accountSensor.CapacityL.HasValue)
                return levelFraction.Value * _accountSensor.CapacityL.Value;

            return null;
        }
    }
}

public class MeasurementEx
{
    private readonly Core.Entities.AccountSensor _accountSensor;
    private readonly Measurement _measurement;

    public MeasurementEx(Measurement measurement, Core.Entities.AccountSensor accountSensor)
    {
        _measurement = measurement;
        _accountSensor = accountSensor;
    }

    public string DevEui => _measurement.DevEui;
    public DateTime Timestamp => _measurement.Timestamp;
    public MeasurementDistance Distance => new(_measurement.DistanceMm, _accountSensor);
    public double BatV => _measurement.BatV;
    public double RssiDbm => _measurement.RssiDbm;
    public double RssiPrc => (_measurement.RssiDbm + 150.0) / 60.0 * 80.0;
    public double BatteryPrc => (_measurement.BatV - 3.0) / 0.335 * 100.0;
}

public class MeasurementAggEx
{
    private readonly Core.Entities.AccountSensor _accountSensor;
    private readonly MeasurementAgg _measurement;

    public MeasurementAggEx(MeasurementAgg measurement, Core.Entities.AccountSensor accountSensor)
    {
        _measurement = measurement;
        _accountSensor = accountSensor;
    }

    public string DevEui => _measurement.DevEui;
    public DateTime Timestamp => _measurement.Timestamp;
    public MeasurementDistance MinDistance => new(_measurement.MinDistanceMm, _accountSensor);
    public MeasurementDistance MeanDistance => new(_measurement.MeanDistanceMm, _accountSensor);
    public MeasurementDistance MaxDistance => new(_measurement.MaxDistanceMm, _accountSensor);
    public MeasurementDistance LastDistance => new(_measurement.LastDistanceMm, _accountSensor);
    public double BatV => _measurement.BatV;
    public double RssiDbm => _measurement.RssiDbm;
    public double RssiPrc => (_measurement.RssiDbm + 150.0) / 60.0 * 80.0;
    public double BatteryPrc => (_measurement.BatV - 3.0) / 0.335 * 100.0;

    public double? LastRealLevelFraction
    {
        get
        {
            if (_accountSensor is { DistanceMmEmpty: not null, DistanceMmFull: not null })
                return ((double)_accountSensor.DistanceMmEmpty.Value - _measurement.LastDistanceMm)
                       / (_accountSensor.DistanceMmEmpty.Value - _accountSensor.DistanceMmFull.Value);
            return null;
        }
    }

    public double? LevelFraction
    {
        get
        {
            var realLevelFraction = LastRealLevelFraction;
            if (!realLevelFraction.HasValue)
                return null;
            if (realLevelFraction.Value > 1.0)
                return 1.0;
            if (realLevelFraction.Value < 0.0)
                return 0.0;
            return realLevelFraction;
        }
    }

    public double? WaterL
    {
        get
        {
            var levelFraction = LevelFraction;
            if (levelFraction != null && _accountSensor.CapacityL.HasValue)
                return levelFraction.Value * _accountSensor.CapacityL.Value;

            return null;
        }
    }
}

public class AccountSensor : PageModel
{
    private readonly IMediator _mediator;

    public AccountSensor(IMediator mediator)
    {
        _mediator = mediator;
    }

    public MeasurementEx? LastMeasurement { get; private set; }
    public MeasurementAggEx[]? Measurements { get; private set; }

    public double? ResolutionL { get; private set; }

    public Core.Entities.AccountSensor? AccountSensorEntity { get; private set; }

    public async Task OnGet(string accountLink, string sensorLink)
    {
        AccountSensorEntity = await _mediator.Send(new SensorByLinkQuery
        {
            SensorLink = sensorLink,
            AccountLink = accountLink
        });

        if (AccountSensorEntity != null)
        {
            if (AccountSensorEntity.DistanceMmEmpty.HasValue && AccountSensorEntity.DistanceMmFull.HasValue &&
                AccountSensorEntity.CapacityL.HasValue)
                ResolutionL =
                    1.0 / (AccountSensorEntity.DistanceMmEmpty.Value - AccountSensorEntity.DistanceMmFull.Value)
                    * AccountSensorEntity.CapacityL.Value;

            var lastMeasurement = await _mediator.Send(new LastMeasurementQuery
                { DevEui = AccountSensorEntity.Sensor.DevEui });
            if (lastMeasurement != null) LastMeasurement = new MeasurementEx(lastMeasurement, AccountSensorEntity);

            Measurements = (await _mediator.Send(new MeasurementsQuery
                {
                    DevEui = AccountSensorEntity.Sensor.DevEui,
                    From = DateTime.UtcNow.AddDays(-7.0)
                }))
                .OrderBy(m => m.Timestamp)
                .Select(m => new MeasurementAggEx(m, AccountSensorEntity))
                .ToArray();
        }
    }
}