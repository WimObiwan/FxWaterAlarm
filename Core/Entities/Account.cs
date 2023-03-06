namespace Core.Entities;

public class Account
{
    private readonly List<AccountSensor> _accountSensors = null!;
    private readonly List<Sensor> _sensors = null!;
    public int Id { get; } = 0;
    public required Guid Uid { get; init; }
    public required string Email { get; init; }
    public string? Name { get; set; }
    public IReadOnlyCollection<AccountSensor> AccountSensors => _accountSensors.AsReadOnly();
    public IReadOnlyCollection<Sensor> Sensors => _sensors.AsReadOnly();

    public void AddSensor(Sensor sensor)
    {
        _accountSensors.Add(
            new AccountSensor
            {
                Account = this,
                Sensor = sensor
            });
    }
}