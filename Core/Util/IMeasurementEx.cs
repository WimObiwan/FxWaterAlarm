using Core.Entities;

namespace Core.Util;

public interface IMeasurementEx
{
    DateTime EstimateNextRefresh();
    AccountSensor AccountSensor { get; }
    string DevEui { get; }
    DateTime Timestamp { get; }
    double BatV { get; }
    double RssiDbm { get; }
    double RssiPrc { get; }
    double BatteryPrc { get; }
}