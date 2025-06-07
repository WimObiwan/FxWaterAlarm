using Microsoft.Extensions.Configuration;

namespace Core.Helpers;

public interface IUrlBuilder
{
    string BuildUrl(string? restPath = null);
}

public class UrlBuilder : IUrlBuilder
{
    public readonly IConfiguration _configuration;

    public UrlBuilder(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string BuildUrl(string? restPath = null)
    {
        string url = _configuration["BaseUrl"]?.TrimEnd('/') ?? "https://wateralarm.be";

        if (!string.IsNullOrEmpty(restPath))
            url += restPath;

        return url;
    }
}