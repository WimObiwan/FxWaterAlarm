using System.Globalization;
using Core.Communication;
using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using Core.Util;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Commands;

public record CheckAccountSensorAlarmsCommand : IRequest
{
    public required Guid AccountUid { get; init; }
    public required Guid SensorUid { get; init; }
}

public class CheckAccountSensorAlarmsCommandHandler : CheckAccountSensorAlarmsCommandHandlerBase, IRequestHandler<CheckAccountSensorAlarmsCommand>
{
    private readonly WaterAlarmDbContext _dbContext;

    public CheckAccountSensorAlarmsCommandHandler(WaterAlarmDbContext dbContext, IMeasurementRepository measurementRepository, IMessenger messenger, 
        ILogger<CheckAccountSensorAlarmsCommandHandler> logger)
        : base(measurementRepository, messenger, logger)
    {
        _dbContext = dbContext;
    }

    public async Task Handle(CheckAccountSensorAlarmsCommand request, CancellationToken cancellationToken)
    {
        var accountSensor =
            await _dbContext.Accounts
                .Where(a => a.Uid == request.AccountUid)
                .SelectMany(a => a.AccountSensors)
                .Where(@as => @as.Sensor.Uid == request.SensorUid)
                .Include(@as => @as.Sensor)
                .Include(@as => @as.Account)
                .SingleOrDefaultAsync(cancellationToken)
            ?? throw new AccountSensorNotFoundException("The accountsensor cannot be found.") 
            { AccountUid = request.AccountUid, SensorUid = request.SensorUid };

        await CheckAccountSensorAlarms(accountSensor, cancellationToken);
    }
}

public abstract class CheckAccountSensorAlarmsCommandHandlerBase
{
    private readonly IMeasurementRepository _measurementRepository;
    private readonly IMessenger _messenger;
    private readonly ILogger _logger;

    public CheckAccountSensorAlarmsCommandHandlerBase(IMeasurementRepository measurementRepository, IMessenger messenger, ILogger logger)
    {
        _measurementRepository = measurementRepository;
        _messenger = messenger;
        _logger = logger;
    }

    protected async Task CheckAccountSensorAlarms(AccountSensor accountSensor, CancellationToken cancellationToken)
    {
        if (!accountSensor.AlertsEnabled)
        {
            _logger.LogWarning("Alerts disabled for accountSensor");
            return;
        }

        // TODO: Use GetLastMedian
        //var medianFrom = DateTime.Now.AddHours(-24).ToUniversalTime();
        var measurement = await _measurementRepository.GetLast(accountSensor.Sensor.DevEui, cancellationToken)
            ?? throw new Exception("No measurement found");
        
        var measurementEx = new MeasurementEx(measurement, accountSensor);
        await CheckAccountSensorAlarms(measurementEx);
    }

    private async Task CheckAccountSensorAlarms(MeasurementEx measurementEx)
    {
        const double thresholdDataHours = 24.5;
        var thresholdData = TimeSpan.FromHours(thresholdDataHours);
        if (DateTime.UtcNow - measurementEx.Timestamp > thresholdData)
            await SendAlert(AlertType.Data, measurementEx.AccountSensor, measurementEx.Timestamp, thresholdData);

        // const double thresholdBatteryPrc = 15.0;
        // if (measurementEx.BatteryPrc < thresholdBatteryPrc)
        //     await SendAlert(AlertType.Battery, measurementEx.AccountSensor, measurementEx.BatteryPrc, thresholdBatteryPrc);

        // const double thresholdLevelFractionLow1 = 0.15;
        const double thresholdLevelFractionLow2 = 0.25;
        //const double thresholdLevelFractionHigh = 1.00;
        if (measurementEx.Distance.LevelFraction is {} levelFraction)
        {
            // if (levelFraction <= thresholdLevelFractionLow1)
            //     await SendAlert(AlertType.LevelFractionLow, measurementEx.AccountSensor, levelFraction * 100.0, thresholdLevelFractionLow1);
            // else
            if (measurementEx.Distance.LevelFraction <= thresholdLevelFractionLow2)
                await SendAlert(AlertType.LevelFractionLow, measurementEx.AccountSensor, levelFraction * 100.0, thresholdLevelFractionLow2);
            // else if (measurementEx.Distance.LevelFraction >= thresholdLevelFractionHigh)
            //     await SendAlert(AlertType.LevelFractionHigh, measurementEx.AccountSensor, levelFraction * 100.0, thresholdLevelFractionHigh);
            // else
            //     await SendAlert(AlertType.LevelFractionStatus, measurementEx.AccountSensor, levelFraction * 100.0);
        }
    }

    private async Task SendAlert(AlertType alertType, AccountSensor accountSensor, DateTime value, TimeSpan? thresholdValue = null)
    {
        var culture = CultureInfo.GetCultureInfo("nl-BE");
        var dateTimeString = value.ToLocalTime().ToString("f", culture);

        string message, shortMessage;
        switch (alertType)
        {
            case AlertType.Data:
                message = $"Er werden geen gegevens ontvangen van uw sensor sinds <strong>{dateTimeString}</strong>";
                shortMessage = "Geen gegevens ontvangen";
                break;
            default:
                throw new InvalidOperationException();
        }

        await SendAlert(accountSensor, message, shortMessage);
    }

    private async Task SendAlert(AlertType alertType, AccountSensor accountSensor, double value, double? thresholdValue = null)
    {
        var culture = CultureInfo.GetCultureInfo("nl-BE");
        var valueString = value.ToString("0", culture);

        string message, shortMessage;
        switch (alertType)
        {
            case AlertType.Battery:
                message = $"De geschatte batterij-capaciteit is gezakt naar <strong>{valueString}%</strong>";
                shortMessage = $"Batterij {valueString}%";
                break;
            case AlertType.LevelFractionHigh:
                message = $"Het gemeten niveau van de sensor is gestegen naar <strong>{valueString}%</strong>";
                shortMessage = $"Niveau {valueString}%";
                break;
            case AlertType.LevelFractionLow:
                message = $"Het gemeten niveau van de sensor is gezakt naar <strong>{valueString}%</strong>";
                shortMessage = $"Niveau {valueString}%";
                break;
            case AlertType.LevelFractionStatus:
                message = $"Het gemeten niveau van de sensor is <strong>{valueString}%</strong>";
                shortMessage = $"Niveau {valueString}%";
                break;
            default:
                throw new InvalidOperationException();
        }

        await SendAlert(accountSensor, message, shortMessage);
    }

    private async Task SendAlert(AccountSensor accountSensor, string message, string shortMessage)
    {
        string email = accountSensor.Account.Email;

        //TODO
        string url = "https://www.wateralarm.be" + accountSensor.RestPath;

        await _messenger.SendAlertMailAsync(email, url, accountSensor.Name, message, shortMessage);
    }

    private enum AlertType { Data = 1, Battery = 2, LevelFractionLow = 3, LevelFractionHigh = 4, LevelFractionStatus = 5 }
}