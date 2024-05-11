using Core.Entities;
using Core.Exceptions;
using Core.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Commands;

public abstract class AddDefaultSensorAlarmsCommandHandlerBase
{
    ILogger _logger;

    public AddDefaultSensorAlarmsCommandHandlerBase(ILogger logger)
    {
        _logger = logger;
    }

    public void CreateAlarms(AccountSensor accountSensor)
    {
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

        accountSensor.AddAlarm(new AccountSensorAlarm
        {
            Uid = Guid.NewGuid(),
            AlarmType = AccountSensorAlarmType.LevelFractionLow,
            AlarmThreshold = 25.0
        });
    }
}