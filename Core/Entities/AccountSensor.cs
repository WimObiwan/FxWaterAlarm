namespace Core.Entities;

public class AccountSensor
{
    public required Account Account { get; init; }
    public required Sensor Sensor { get; init; }
    public required DateTime CreateTimestamp { get; init; }

    public string? Name { get; set; }
    public int? DistanceMmEmpty { get; set; }
    public int? DistanceMmFull { get; set; }
    public int? CapacityL { get; set; }

    public double? ResolutionL
    {
        get
        {
            if (!DistanceMmEmpty.HasValue || !DistanceMmFull.HasValue || !CapacityL.HasValue)
                return null;

            return 1.0 / (DistanceMmEmpty.Value - DistanceMmFull.Value) * CapacityL.Value;
        }
    }
}