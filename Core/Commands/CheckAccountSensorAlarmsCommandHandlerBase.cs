using System.Globalization;
using Core.Communication;
using Core.Entities;
using Core.Repositories;
using Core.Util;
using Microsoft.Extensions.Logging;

namespace Core.Commands;

public abstract class CheckAccountSensorAlarmsCommandHandlerBase
{
    private readonly WaterAlarmDbContext _dbContext;
    private readonly IMeasurementRepository _measurementRepository;
    private readonly IMessenger _messenger;
    private readonly ILogger _logger;

    public CheckAccountSensorAlarmsCommandHandlerBase(
        WaterAlarmDbContext dbContext,
        IMeasurementRepository measurementRepository, 
        IMessenger messenger, 
        ILogger logger)
    {
        _dbContext = dbContext;
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

        var alarms = await GetAlarms(accountSensor, cancellationToken);

        if (alarms.Count > 0)
        {
            // TODO: Use GetLastMedian
            //var medianFrom = DateTime.Now.AddHours(-24).ToUniversalTime();
            var measurement = await _measurementRepository.GetLast(accountSensor.Sensor.DevEui, cancellationToken)
                ?? throw new Exception("No measurement found");
            
            var measurementEx = new MeasurementEx(measurement, accountSensor);
            await CheckAccountSensorAlarms(measurementEx, alarms);
        }
        else
        {
            _logger.LogWarning("No alarms found for AccountSensor {AccountUid} {SensorUid}", 
                accountSensor.Account.Uid, accountSensor.Sensor.Uid);
        }
    }

    private async Task CheckAccountSensorAlarms(MeasurementEx measurementEx, IReadOnlyCollection<AccountSensorAlarm> alarms)
    {
        foreach (var alarm in alarms)
        {
            bool? isTriggered = null;
            Func<Task>? sendAlertFunction = null;
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
                        isTriggered = measurementEx.BatteryPrc < alarmThreshold;
                        sendAlertFunction = () => SendAlert(AccountSensorAlarmType.Battery, measurementEx.AccountSensor, measurementEx.BatteryPrc, alarmThreshold);
                    }
                    break;
                }
                case AccountSensorAlarmType.LevelFractionLow:
                {
                    if (!(measurementEx.Distance.LevelFraction is {} levelFraction))
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
                        sendAlertFunction = () => SendAlert(AccountSensorAlarmType.LevelFractionLow, measurementEx.AccountSensor, levelFraction, alarmThreshold);
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
                    _logger.LogInformation("Alarm {Type} {Threshold} is (still) triggered", 
                        alarm.AlarmType, alarm.AlarmThreshold);

                    var sendAlert = await SetAlarmTriggeredIfNeeded(alarm);
                    if (sendAlert && sendAlertFunction != null)
                        await sendAlertFunction();
                }
                else
                {
                    _logger.LogInformation("Alarm {Type} {Threshold} is (still) not triggered", 
                        alarm.AlarmType, alarm.AlarmThreshold);

                    await SetAlarmClearedIfNeeded(alarm);
                }
            }
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

    private async Task<IReadOnlyCollection<AccountSensorAlarm>> GetAlarms(AccountSensor accountSensor, CancellationToken cancellationToken)
    {
        await _dbContext.Entry(accountSensor).Collection(a => a.Alarms).LoadAsync(cancellationToken);
        return accountSensor.Alarms;
        // var alerts = new AccountSensorAlarm[]
        // {
        //     new()
        //     {
        //         AlarmType = AccountSensorAlarmType.Data,
        //         AlarmThreshold = 24.5
        //     },
        //     new()
        //     {
        //         AlarmType = AccountSensorAlarmType.LevelFractionLow,
        //         AlarmThreshold = 25.0
        //     }
        // };

        // return Task.FromResult<IList<AccountSensorAlarm>>(alerts);
    }

    private Task<bool> SetAlarmTriggeredIfNeeded(AccountSensorAlarm alarm)
    {
        return Task.FromResult(true);
    }

    private Task<bool> SetAlarmClearedIfNeeded(AccountSensorAlarm alarm)
    {
        return Task.FromResult(true);
    }
}