using CommandLine;
using Core;
using Core.Queries;
using MediatR;
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

                    // return await Parser.Default
                    //     .ParseArguments<
                    //         HelpOptions,
                    //         QueryLastMeasurementOptions,
                    //         CreateAccountOptions,
                    //         QueryAccountsOptions,
                    //         CreateSensorOptions
                    //     >(args)
                    //     .MapResult(
                    //         (HelpOptions _) => RunHelp(),
                    //         (QueryLastMeasurementOptions o) => QueryLastMeasurement(host, o),
                    //         (CreateAccountOptions o) => CreateAccount(host, o),
                    //         (QueryAccountsOptions o) => QueryAccounts(host, o),
                    //         (CreateSensorOptions o) => CreateSensor(host, o),
                    //         _ => Task.FromResult(1));
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

    private static async Task<int> QueryLastMeasurement(IHost host, QueryLastMeasurementOptions options)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        var mediator = host.Services.GetRequiredService<IMediator>();
        var result = await mediator.Send(
            new LastMeasurementQuery { DevEui = options.DevEui });

        if (result == null)
        {
            logger.LogWarning("No measurement found for DevEui {DevEui}",
                options.DevEui);
            return 1;
        }

        logger.LogInformation("{DevEui} {Timestamp} {DistanceMm} {BatV} {RssiDbm}",
            result.DevEui, result.Timestamp, result.DistanceMm, result.BatV, result.RssiDbm);
        System.Console.WriteLine("{0} {1} {2} {3} {4}",
            result.DevEui, result.Timestamp, result.DistanceMm, result.BatV, result.RssiDbm);

        return 0;
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