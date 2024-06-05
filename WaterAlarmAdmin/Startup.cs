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
    // Override the `ConfigureServices` method to register your own dependencies
    public override void ConfigureServices(IServiceCollection services)
    {
        var builder = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .AddJsonFile("appsettings.Local.json", true, false)
            .AddEnvironmentVariables();
        IConfigurationRoot configuration = builder.Build();

        services.AddWaterAlarmCore(configuration, typeof(Startup).Assembly);
    }

    // Override the `ConfigurePowerShellLogging` method to change the default logging configuration.
    public override Action<PowerShellLoggerConfiguration> ConfigurePowerShellLogging()
    {
        return builder =>
        {
            builder.DefaultLevel = LogLevel.Information;
            builder.LogLevel["Svrooij.PowerShell.DependencyInjection.Sample.TestSampleCmdletCommand"] = LogLevel.Debug;
            builder.IncludeCategory = true;
            builder.StripNamespace = true;
        };
    }
}