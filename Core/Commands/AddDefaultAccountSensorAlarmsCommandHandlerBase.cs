using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Commands;

public abstract class AddDefaultAccountSensorAlarmsCommandHandlerBase
{
    ILogger _logger;

    public AddDefaultAccountSensorAlarmsCommandHandlerBase(ILogger logger)
    {
        _logger = logger;
    }

    public void CreateAlarms(AccountSensor accountSensor)
    {
        accountSensor.EnsureEnabled();

        if (accountSensor.Alarms.Count > 0)
        {
            _logger.LogWarning("Skip accountsensor {AccountUid} {SensorUid} because there are already alarms",
                accountSensor.Account.Uid, accountSensor.Sensor.Uid);
            return;
        }

        accountSensor.AddAlarm(new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.Data,
            AlarmThreshold = 24.5
        });


        switch (accountSensor.Sensor.Type)
        {
            case SensorType.Level:
                CreateLevelAlarms(accountSensor);
                break;
            case SensorType.Detect:
                CreateDetectAlarms(accountSensor);
                break;
            case SensorType.Moisture:
                CreateMoistureAlarms(accountSensor);
                break;
            case SensorType.Thermometer:
                CreateThermometerAlarms(accountSensor);
                break;
            default:
                _logger.LogWarning("Skip accountsensor {AccountUid} {SensorUid} because the sensor type is not supported",
                    accountSensor.Account.Uid, accountSensor.Sensor.Uid);
                break;
        }
    }

    private void CreateLevelAlarms(AccountSensor accountSensor)
    {
        accountSensor.AddAlarm(new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.PercentageLow,
            AlarmThreshold = 25.0
        });
    }

    private void CreateDetectAlarms(AccountSensor accountSensor)
    {
        accountSensor.AddAlarm(new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.DetectOn,
        });
    }

    private void CreateMoistureAlarms(AccountSensor accountSensor)
    {
    }

    private void CreateThermometerAlarms(AccountSensor accountSensor)
    {
    }
}