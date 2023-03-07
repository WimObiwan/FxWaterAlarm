using Console.ConsoleCommands;
using Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Console;

public class Startup
{
    private readonly IConfiguration _configuration;

    public Startup(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<ICommandLineEngine, CommandLineEngine>();
        services.AddScoped<IConsoleCommand, AccountConsoleCommand>();
        services.AddScoped<IConsoleCommand, SensorConsoleCommand>();

        services.AddWaterAlarmCore(_configuration, typeof(Program).Assembly);
    }
}