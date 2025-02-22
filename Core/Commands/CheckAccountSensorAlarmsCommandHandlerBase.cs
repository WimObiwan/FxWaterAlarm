using System.Globalization;
using Core.Communication;
using Core.Entities;
using Core.Queries;
using Core.Repositories;
using Core.Util;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Core.Commands;

public abstract class CheckAccountSensorAlarmsCommandHandlerBase
{
    private readonly WaterAlarmDbContext _dbContext;
    private readonly IMediator _mediator;
    private readonly IMessenger _messenger;
    private readonly ILogger _logger;

    public CheckAccountSensorAlarmsCommandHandlerBase(
        WaterAlarmDbContext dbContext,
        IMediator mediator,
        IMessenger messenger, 
        ILogger logger)
    {
        _dbContext = dbContext;
        _mediator = mediator;  // I know... https://lostechies.com/jimmybogard/2016/12/12/dealing-with-duplication-in-mediatr-handlers/
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

        var alarms = await GetAlarms(accountSensor, cancellationToken);

        if (alarms.Count > 0)
        {
            // TODO: Use GetLastMedian
            //var medianFrom = DateTime.Now.AddHours(-24).ToUniversalTime();

            var measurementEx = await _mediator.Send(
                new LastMeasurementQuery 
                {
                    AccountSensor = accountSensor
                }, cancellationToken)
                ?? throw new Exception("No measurement found");
            
            await CheckAccountSensorAlarms(measurementEx, alarms, cancellationToken);
        }
        else
        {
            _logger.LogWarning("No alarms found for AccountSensor {AccountUid} {SensorUid}", 
                accountSensor.Account.Uid, accountSensor.Sensor.Uid);
        }
    }

    private async Task CheckAccountSensorAlarms(IMeasurementEx measurementEx, IReadOnlyCollection<AccountSensorAlarm> alarms, 
        CancellationToken cancellationToken)
    {
        const double AlarmThresholdHisteresisBattery = 5.0;
        const double AlarmThresholdHisteresisPercentage = 5.0;
        const int AlarmThresholdHisteresisHeight = 50;

        foreach (var alarm in alarms)
        {
            bool isTriggered = false;
            bool isCleared = false;
            Func<Task>? sendAlertFunction = null;
            Func<Task>? sendAlertClearedFunction = null;
            switch (alarm.AlarmType)
            {
                case AccountSensorAlarmType.Data:
                {
                    if (!(alarm.AlarmThreshold is {} alarmThreshold))
                    {
                        _logger.LogWarning("No threshold configured for alarm {AlarmType}", alarm.AlarmType);
                    }
                    else
                    {
                        var thresholdData = TimeSpan.FromHours(alarmThreshold);
                        isTriggered = DateTime.UtcNow - measurementEx.Timestamp > thresholdData;
                        isCleared = !isTriggered;
                        sendAlertFunction = () => SendAlert(AccountSensorAlarmType.Data, measurementEx.AccountSensor, measurementEx.Timestamp, thresholdData);
                    }
                    break;
                }
                case AccountSensorAlarmType.Battery:
                {
                    if (!(alarm.AlarmThreshold is {} alarmThreshold))
                    {
                        _logger.LogWarning("No threshold configured for alarm {AlarmType}", alarm.AlarmType);
                    }
                    else
                    {
                        isTriggered = measurementEx.BatteryPrc <= alarmThreshold;
                        isCleared = measurementEx.BatteryPrc > alarmThreshold + AlarmThresholdHisteresisBattery;
                        sendAlertFunction = () => SendAlert(AccountSensorAlarmType.Battery, measurementEx.AccountSensor, measurementEx.BatteryPrc, alarmThreshold);
                    }
                    break;
                }
                case AccountSensorAlarmType.PercentageLow:
                {
                    if (!(measurementEx is MeasurementLevelEx measurementLevelEx))
                    {
                        _logger.LogWarning("Last measurement is not a level measurement");
                    }
                    else if (!(measurementLevelEx.Distance.LevelFraction is {} levelFraction))
                    {
                        _logger.LogWarning("No LevelFraction");
                    }
                    else if (!(alarm.AlarmThreshold is {} alarmThreshold))
                    {
                        _logger.LogWarning("No threshold configured for alarm {AlarmType}", alarm.AlarmType);
                    }
                    else
                    {
                        levelFraction *= 100.0;
                        isTriggered = levelFraction <= alarmThreshold;
                        isCleared = levelFraction > alarmThreshold + AlarmThresholdHisteresisPercentage;
                        sendAlertFunction = () => SendAlert(AccountSensorAlarmType.PercentageLow, measurementEx.AccountSensor, levelFraction, alarmThreshold);
                    }
                    break;
                }
                case AccountSensorAlarmType.PercentageHigh:
                {
                    if (!(measurementEx is MeasurementLevelEx measurementLevelEx))
                    {
                        _logger.LogWarning("Last measurement is not a level measurement");
                    }
                    else if (!(measurementLevelEx.Distance.LevelFraction is {} levelFraction))
                    {
                        _logger.LogWarning("No LevelFraction");
                    }
                    else if (!(alarm.AlarmThreshold is {} alarmThreshold))
                    {
                        _logger.LogWarning("No threshold configured for alarm {AlarmType}", alarm.AlarmType);
                    }
                    else
                    {
                        levelFraction *= 100.0;
                        isTriggered = levelFraction >= alarm.AlarmThreshold;
                        isCleared = levelFraction < alarmThreshold - AlarmThresholdHisteresisPercentage;
                        sendAlertFunction = () => SendAlert(AccountSensorAlarmType.PercentageHigh, measurementEx.AccountSensor, levelFraction, alarmThreshold);
                    }
                    break;
                }
                // case AccountSensorAlarmType.PercentageStatus:
                // {
                //     if (!(measurementEx.Distance.LevelFraction is {} levelFraction))
                //     {
                //         _logger.LogWarning("No LevelFraction");
                //     }
                //     else
                //     {
                //         levelFraction *= 100.0;
                //         isTriggered = true;
                //         isCleared = false;
                //         sendAlertFunction = () => SendAlert(AccountSensorAlarmType.PercentageStatus, measurementEx.AccountSensor, levelFraction, 0.0);
                //     }
                //     break;
                // }
                case AccountSensorAlarmType.HeightLow:
                {
                    if (!(measurementEx is MeasurementLevelEx measurementLevelEx))
                    {
                        _logger.LogWarning("Last measurement is not a level measurement");
                    }
                    else if (!(measurementLevelEx.Distance.HeightMm is {} heightMm))
                    {
                        _logger.LogWarning("No Height");
                    }
                    else if (!(alarm.AlarmThreshold is {} alarmThreshold))
                    {
                        _logger.LogWarning("No threshold configured for alarm {AlarmType}", alarm.AlarmType);
                    }
                    else
                    {
                        isTriggered = heightMm <= alarmThreshold;
                        isCleared = heightMm > alarmThreshold + AlarmThresholdHisteresisHeight;
                        sendAlertFunction = () => SendAlert(AccountSensorAlarmType.HeightLow, measurementEx.AccountSensor, heightMm, alarmThreshold);
                    }
                    break;
                }
                case AccountSensorAlarmType.HeightHigh:
                {
                    if (!(measurementEx is MeasurementLevelEx measurementLevelEx))
                    {
                        _logger.LogWarning("Last measurement is not a level measurement");
                    }
                    else if (!(measurementLevelEx.Distance.HeightMm is {} heightMm))
                    {
                        _logger.LogWarning("No Height");
                    }
                    else if (!(alarm.AlarmThreshold is {} alarmThreshold))
                    {
                        _logger.LogWarning("No threshold configured for alarm {AlarmType}", alarm.AlarmType);
                    }
                    else
                    {
                        isTriggered = heightMm >= alarm.AlarmThreshold;
                        isCleared = heightMm < alarmThreshold - AlarmThresholdHisteresisHeight;
                        sendAlertFunction = () => SendAlert(AccountSensorAlarmType.HeightHigh, measurementEx.AccountSensor, heightMm, alarmThreshold);
                    }
                    break;
                }
                // case AccountSensorAlarmType.HeightStatus:
                // {
                //     if (!(measurementEx.Distance.HeightMm is {} heightMm))
                //     {
                //         _logger.LogWarning("No Height");
                //     }
                //     else
                //     {
                //         isTriggered = true;
                //         isCleared = false;
                //         sendAlertFunction = () => SendAlert(AccountSensorAlarmType.HeightStatus, measurementEx.AccountSensor, heightMm, 0.0);
                //     }
                //     break;
                // }
                case AccountSensorAlarmType.DetectOn:
                {
                    if (!(measurementEx is MeasurementDetectEx measurementDetectEx))
                    {
                        _logger.LogWarning("Last measurement is not a detect measurement");
                    }
                    else
                    {
                        isTriggered = measurementDetectEx.Status == 1;
                        isCleared = measurementDetectEx.Status == 0;
                        sendAlertFunction = () => SendAlert(AccountSensorAlarmType.DetectOn, measurementEx.AccountSensor, measurementDetectEx.Status);
                        sendAlertClearedFunction = () => SendAlertCleared(AccountSensorAlarmType.DetectOn, measurementEx.AccountSensor, measurementDetectEx.Status);
                    }
                    break;
                }
                // case AccountSensorAlarmType.DetectStatus:
                // {
                //     ...
                //     break;
                // }
                default:
                    _logger.LogError("AlarmType {AlarmType} not implemented", alarm.AlarmType);
                    break;
            }

            if (isTriggered)
            {
                _logger.LogInformation("Alarm {Type} {Threshold} is (still) triggered", 
                    alarm.AlarmType, alarm.AlarmThreshold);

                var sendAlert = await SetAlarmTriggeredIfNeeded(alarm, cancellationToken);
                if (sendAlert && sendAlertFunction != null)
                    await sendAlertFunction();

                if (isCleared)
                    _logger.LogWarning("Triggered and Cleared should never be both true.  Only using Triggered.");
            }
            else if (isCleared)
            {
                _logger.LogInformation("Alarm {Type} {Threshold} is (still) not triggered", 
                    alarm.AlarmType, alarm.AlarmThreshold);

                var sendAlert = await SetAlarmClearedIfNeeded(alarm, cancellationToken);
                if (sendAlert && sendAlertClearedFunction != null)
                    await sendAlertClearedFunction();
            }
        }
    }

    private async Task SendAlert(AccountSensorAlarmType alertType, AccountSensor accountSensor, DateTime value, TimeSpan thresholdValue)
    {
        var culture = CultureInfo.GetCultureInfo("nl-BE");
        var dateTimeString = value.ToLocalTime().ToString("f", culture);

        string message, shortMessage;
        switch (alertType)
        {
            case AccountSensorAlarmType.Data:
                message = $"Er werden geen gegevens ontvangen van uw sensor sinds <strong>{dateTimeString}</strong>";
                shortMessage = $"Geen gegevens ontvangen sinds {dateTimeString}";
                break;
            default:
                throw new InvalidOperationException();
        }

        await SendAlert(accountSensor, message, shortMessage);
    }

    private async Task SendAlert(AccountSensorAlarmType alertType, AccountSensor accountSensor, double value, double thresholdValue)
    {
        var culture = CultureInfo.GetCultureInfo("nl-BE");
        var valueString = value.ToString("0", culture);

        string message, shortMessage;
        switch (alertType)
        {
            case AccountSensorAlarmType.Battery:
                message = $"De geschatte batterij-capaciteit is gezakt onder <strong>{thresholdValue}%</strong>";
                shortMessage = $"Batterij {valueString}%";
                break;
            case AccountSensorAlarmType.PercentageHigh:
                message = $"Het gemeten niveau van de sensor is gestegen boven <strong>{thresholdValue}%</strong>";
                shortMessage = $"Niveau {valueString}%";
                break;
            case AccountSensorAlarmType.PercentageLow:
                message = $"Het gemeten niveau van de sensor is gezakt onder <strong>{thresholdValue}%</strong>";
                shortMessage = $"Niveau {valueString}%";
                break;
            // case AccountSensorAlarmType.PercentageStatus:
            //     message = $"Het gemeten niveau van de sensor is <strong>{valueString}%</strong>";
            //     shortMessage = $"Niveau {valueString}%";
            //     break;
            case AccountSensorAlarmType.HeightHigh:
                message = $"Het gemeten niveau van de sensor is gestegen boven <strong>{thresholdValue} mm</strong>";
                shortMessage = $"Niveau {valueString} mm";
                break;
            case AccountSensorAlarmType.HeightLow:
                message = $"Het gemeten niveau van de sensor is gezakt onder <strong>{thresholdValue} mm</strong>";
                shortMessage = $"Niveau {valueString} mm";
                break;
            // case AccountSensorAlarmType.HeightStatus:
            //     message = $"Het gemeten niveau van de sensor is <strong>{valueString} mm</strong>";
            //     shortMessage = $"Niveau {valueString} mm";
            //     break;
            default:
                throw new InvalidOperationException();
        }

        await SendAlert(accountSensor, message, shortMessage);
    }

    private async Task SendAlert(AccountSensorAlarmType alertType, AccountSensor accountSensor, int value)
    {
        string message, shortMessage;
        switch (alertType)
        {
            case AccountSensorAlarmType.DetectOn:
                message = "De sensor heeft water gedetecteerd";
                shortMessage = "Water gedetecteerd";
                break;
            // case AccountSensorAlarmType.DetectStatus:
            //     ...
            //     break;
            default:
                throw new InvalidOperationException();
        }

        await SendAlert(accountSensor, message, shortMessage);
    }

    private async Task SendAlertCleared(AccountSensorAlarmType alertType, AccountSensor accountSensor, int value)
    {
        string message, shortMessage;
        switch (alertType)
        {
            case AccountSensorAlarmType.DetectOn:
                message = "De sensor heeft geen water meer gedetecteerd";
                shortMessage = "Geen water gedetecteerd";
                break;
            // case AccountSensorAlarmType.DetectStatus:
            //     ...
            //     break;
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

    private async Task<IReadOnlyCollection<AccountSensorAlarm>> GetAlarms(AccountSensor accountSensor, CancellationToken cancellationToken)
    {
        await _dbContext.Entry(accountSensor).Collection(a => a.Alarms).LoadAsync(cancellationToken);
        return accountSensor.Alarms;
    }

    private async Task<bool> SetAlarmTriggeredIfNeeded(AccountSensorAlarm alarm, CancellationToken cancellationToken)
    {
        if (alarm.IsCurrentlyTriggered)
        {
            _logger.LogInformation("Alarm is triggered since {LastTriggered}", alarm.LastTriggered);
            return false;
        }

        _logger.LogInformation("Alarm is now triggered");
        alarm.LastTriggered = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }

    private async Task<bool> SetAlarmClearedIfNeeded(AccountSensorAlarm alarm, CancellationToken cancellationToken)
    {
        if (alarm.IsCurrentlyCleared)
        {
            _logger.LogInformation("Alarm is cleared since {LastTriggered}", alarm.LastCleared);
            return false;
        }

        _logger.LogInformation("Alarm is now cleared");
        alarm.LastCleared = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        return true;
    }
}