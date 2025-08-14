namespace Site;

public class ApiKeysOptions
{
    public const string Location = "ApiKeys";

    public List<string> ValidKeys { get; set; } = new();
}