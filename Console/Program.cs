using CommandLine;
using Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Sentry;
using Serilog;
using Serilog.Events;

namespace Console;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            using var host = CreateHostBuilder().Build();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            host.MigrateWaterAlarmDb();

            var versionInfo = host.Services.GetRequiredService<IVersionInfo>();
            logger.LogInformation("Version {Version}, running on {DotNetCoreVersion}", versionInfo.Version,
                versionInfo.DotNetCoreVersion);

            using (SentrySdk.Init(o =>
                   {
                       o.Dsn = null;
                       o.TracesSampleRate = 1.0;
                   }))
            {
                try
                {
                    var engine = host.Services.GetRequiredService<ICommandLineEngine>();
                    return await engine.Execute(args);
                }
                catch (Exception x)
                {
                    SentrySdk.CaptureException(x);
                    throw;
                }
            }
        }
        catch (Exception x)
        {
            Log.Fatal(x, "Host terminated unexpectedly");
            return 1;
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

    private static IHostBuilder CreateHostBuilder()
    {
        return Host.CreateDefaultBuilder()
            .UseSerilog()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config
                    .AddJsonFile("appsettings.Local.json", true, false);
            })
            .UseStartup<Startup>();
    }

    private static Task<int> RunHelp()
    {
        return Task.FromResult(0);
    }

    // ReSharper disable AutoPropertyCanBeMadeGetOnly.Local

    [Verb("QueryLastMeasurement", HelpText = "Query last measurement.")]
    private class QueryLastMeasurementOptions
    {
        [Option('d', "deveui", Required = true, HelpText = "Device 'DevEUI' (Device Extended Unique Identifier)")]
        public string DevEui { get; set; } = default!;
    }

    // ReSharper restore AutoPropertyCanBeMadeGetOnly.Local
}