using System;
using System.IO;
using Core;
using Core.Commands;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Svrooij.PowerShell.DependencyInjection;
using Svrooij.PowerShell.DependencyInjection.Logging;

// https://svrooij.io/2024/01/18/dependencies-powershell-module-csharp/ 

// It has to be a public class with `PsStartup` as base class
public class Startup : PsStartup
{
    private IConfigurationRoot configurationRoot;

    public Startup()
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", true, false)
            .AddEnvironmentVariables();

        configurationRoot = builder.Build();
    }

    // Override the `ConfigureServices` method to register your own dependencies
    public override void ConfigureServices(IServiceCollection services)
    {

        services.AddWaterAlarmCore(configurationRoot, typeof(Startup).Assembly);
    }

    // Override the `ConfigurePowerShellLogging` method to change the default logging configuration.
    public override Action<PowerShellLoggerConfiguration> ConfigurePowerShellLogging()
    {
        return builder =>
        {
            // builder.AddConfiguration(configurationRoot.GetSection("Logging"));

            builder.DefaultLevel = LogLevel.Information;
            builder.LogLevel["Microsoft"] = LogLevel.Warning;
            builder.IncludeCategory = true;
            //builder.StripNamespace = true;
        };
    }
}