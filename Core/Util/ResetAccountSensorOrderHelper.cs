using Core.Entities;
using Microsoft.Extensions.Logging;

namespace Core.Util;

internal static class ResetAccountSensorOrderHelper
{
    public static bool ResetOrder(ILogger? logger, Account account, AccountSensor? accountSensorToPrefer = null)
    {
        int order = 0;
        bool changed = false;

        IEnumerable<AccountSensor> orderedAccountSensors;

        if (accountSensorToPrefer == null) 
            orderedAccountSensors = account.AccountSensors
                .OrderBy(@as => @as.Order);
        else
            orderedAccountSensors = account.AccountSensors
                .OrderBy(@as => @as.Order)
                .ThenBy(@as => @as == accountSensorToPrefer ? 0 : 1);

        foreach (var accountSensor in orderedAccountSensors) 
        {
            if (accountSensor.Order != order)
            {
                logger?.LogInformation("Resetting order for account sensor {AccountSensor} of {Account} from {OldOrder} to {NewOrder}", 
                    accountSensor.Name, account.Email, accountSensor.Order, order);
                changed = true;
                accountSensor.Order = order;
            }

            order++;
        }

        return changed;
    }
}