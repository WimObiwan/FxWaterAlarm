using System.Globalization;
using Core.Communication;
using Core.Entities;
using Core.Repositories;
using Core.Util;
using Microsoft.Extensions.Logging;

namespace Core.Commands;

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

        var alarms = await GetAlarms(accountSensor);

        if (alarms.Count > 0)
        {
            // TODO: Use GetLastMedian
            //var medianFrom = DateTime.Now.AddHours(-24).ToUniversalTime();
            var measurement = await _measurementRepository.GetLast(accountSensor.Sensor.DevEui, cancellationToken)
                ?? throw new Exception("No measurement found");
            
            var measurementEx = new MeasurementEx(measurement, accountSensor);
            await CheckAccountSensorAlarms(measurementEx, alarms);
        }
    }

    private async Task CheckAccountSensorAlarms(MeasurementEx measurementEx, IList<AccountSensorAlarm> alarms)
    {
        foreach (var alarm in alarms)
        {
            bool? isTriggered = null;
            Func<Task>? sendAlertFunction = null;
            switch (alarm.AlarmType)
            {
                case AccountSensorAlarmType.Data:
                {
                    var thresholdData = TimeSpan.FromHours(alarm.AlarmThreshold);
                    isTriggered = DateTime.UtcNow - measurementEx.Timestamp > thresholdData;
                    sendAlertFunction = () => SendAlert(AccountSensorAlarmType.Data, measurementEx.AccountSensor, measurementEx.Timestamp, thresholdData);
                    break;
                }
                case AccountSensorAlarmType.Battery:
                {
                    isTriggered = measurementEx.BatteryPrc < alarm.AlarmThreshold;
                    sendAlertFunction = () => SendAlert(AccountSensorAlarmType.Battery, measurementEx.AccountSensor, measurementEx.BatteryPrc, alarm.AlarmThreshold);
                    break;
                }
                case AccountSensorAlarmType.LevelFractionLow:
                {
                    if (measurementEx.Distance.LevelFraction is {} levelFraction)
                    {
                        levelFraction *= 100.0;
                        isTriggered = levelFraction <= alarm.AlarmThreshold;
                        sendAlertFunction = () => SendAlert(AccountSensorAlarmType.LevelFractionLow, measurementEx.AccountSensor, levelFraction, alarm.AlarmThreshold);
                    }
                    break;
                }
                case AccountSensorAlarmType.LevelFractionHigh:
                {
                    if (measurementEx.Distance.LevelFraction is {} levelFraction)
                    {
                        levelFraction *= 100.0;
                        isTriggered = levelFraction >= alarm.AlarmThreshold;
                        sendAlertFunction = () => SendAlert(AccountSensorAlarmType.LevelFractionHigh, measurementEx.AccountSensor, levelFraction, alarm.AlarmThreshold);
                    }
                    break;
                }
                default:
                    _logger.LogError("AlarmType {AlarmType} not implemented", alarm.AlarmType);
                    break;
            }

            if (isTriggered.HasValue)
            {
                if (isTriggered.Value)
                {
                    var sendAlert = await SetAlarmTriggeredIfNeeded(alarm);
                    if (sendAlert && sendAlertFunction != null)
                        await sendAlertFunction();
                }
                else
                {
                    await SetAlarmClearedIfNeeded(alarm);
                }
            }


            // // const double thresholdLevelFractionLow1 = 0.15;
            // const double thresholdLevelFractionLow2 = 0.25;
            // //const double thresholdLevelFractionHigh = 1.00;
            // if (measurementEx.Distance.LevelFraction is {} levelFraction)
            // {
            //     // if (levelFraction <= thresholdLevelFractionLow1)
            //     //     await SendAlert(AlertType.LevelFractionLow, measurementEx.AccountSensor, levelFraction * 100.0, thresholdLevelFractionLow1);
            //     // else
            //     if (measurementEx.Distance.LevelFraction <= thresholdLevelFractionLow2)
            //         await SendAlert(AlarmType.LevelFractionLow, measurementEx.AccountSensor, levelFraction * 100.0, thresholdLevelFractionLow2);
            //     // else if (measurementEx.Distance.LevelFraction >= thresholdLevelFractionHigh)
            //     //     await SendAlert(AlertType.LevelFractionHigh, measurementEx.AccountSensor, levelFraction * 100.0, thresholdLevelFractionHigh);
            //     // else
            //     //     await SendAlert(AlertType.LevelFractionStatus, measurementEx.AccountSensor, levelFraction * 100.0);
            // }
        }
    }

    private async Task SendAlert(AccountSensorAlarmType alertType, AccountSensor accountSensor, DateTime value, TimeSpan? thresholdValue = null)
    {
        var culture = CultureInfo.GetCultureInfo("nl-BE");
        var dateTimeString = value.ToLocalTime().ToString("f", culture);

        string message, shortMessage;
        switch (alertType)
        {
            case AccountSensorAlarmType.Data:
                message = $"Er werden geen gegevens ontvangen van uw sensor sinds <strong>{dateTimeString}</strong>";
                shortMessage = "Geen gegevens ontvangen";
                break;
            default:
                throw new InvalidOperationException();
        }

        await SendAlert(accountSensor, message, shortMessage);
    }

    private async Task SendAlert(AccountSensorAlarmType alertType, AccountSensor accountSensor, double value, double? thresholdValue = null)
    {
        var culture = CultureInfo.GetCultureInfo("nl-BE");
        var valueString = value.ToString("0", culture);

        string message, shortMessage;
        switch (alertType)
        {
            case AccountSensorAlarmType.Battery:
                message = $"De geschatte batterij-capaciteit is gezakt naar <strong>{valueString}%</strong>";
                shortMessage = $"Batterij {valueString}%";
                break;
            case AccountSensorAlarmType.LevelFractionHigh:
                message = $"Het gemeten niveau van de sensor is gestegen naar <strong>{valueString}%</strong>";
                shortMessage = $"Niveau {valueString}%";
                break;
            case AccountSensorAlarmType.LevelFractionLow:
                message = $"Het gemeten niveau van de sensor is gezakt naar <strong>{valueString}%</strong>";
                shortMessage = $"Niveau {valueString}%";
                break;
            case AccountSensorAlarmType.LevelFractionStatus:
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

    private record AccountSensorAlarm
    {
        public AccountSensorAlarmType AlarmType { get; set; }
        public double AlarmThreshold { get; set; }
        // public DateTime? LastTriggered { get; set; }
        // public DateTime? LastCleared { get; set; }
    }

    private Task<IList<AccountSensorAlarm>> GetAlarms(AccountSensor accountSensor)
    {
        var alerts = new AccountSensorAlarm[]
        {
            new()
            {
                AlarmType = AccountSensorAlarmType.Data,
                AlarmThreshold = 24.5
            },
            new()
            {
                AlarmType = AccountSensorAlarmType.LevelFractionLow,
                AlarmThreshold = 25.0
            }
        };

        return Task.FromResult<IList<AccountSensorAlarm>>(alerts);
    }

    private Task<bool> SetAlarmTriggeredIfNeeded(AccountSensorAlarm alarm)
    {
        return Task.FromResult(true);
    }

    private Task<bool> SetAlarmClearedIfNeeded(AccountSensorAlarm alarm)
    {
        return Task.FromResult(true);
    }

    private enum AccountSensorAlarmType { Data = 1, Battery = 2, LevelFractionLow = 3, LevelFractionHigh = 4, LevelFractionStatus = 5 }
}