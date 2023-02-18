using CommandLine;
using Core;
using Core.Queries;
using Microsoft.EntityFrameworkCore;
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
    [Verb("Help", HelpText = "Shows help about WaterAlarm Console.")]
    private class HelpOptions
    {
    }

    [Verb("QueryLast", HelpText = "Query last measurement.")]
    private class QueryLastOptions
    {
        [Option('d',"deveui", Required = true, HelpText = "Device 'DevEUI' (Device Extended Unique Identifier)")]
        public string DevEui { get; set; } = default!;
    }

    private static async Task<int> Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateLogger();

        try
        {
            using var host = CreateHostBuilder(args).Build();
            var versionInfo = host.Services.GetRequiredService<IVersionInfo>();
            var logger = host.Services.GetRequiredService<ILogger<Program>>();
            logger.LogInformation("Version {Version}, running on {DotNetCoreVersion}", versionInfo.Version,
                versionInfo.DotNetCoreVersion);

            // var serviceProvider = host.Services.GetRequiredService<IServiceProvider>();
            // using (var scope = serviceProvider.CreateScope())
            // {
            //     var waterAlarmDbContext = scope.ServiceProvider.GetRequiredService<WaterAlarmDbContext>();
            //     await waterAlarmDbContext.Database.MigrateAsync();
            // }

            using (SentrySdk.Init(o =>
                   {
                       o.Dsn = null;
                       o.TracesSampleRate = 1.0;
                   }))
            {
                try
                {
                    return await Parser.Default
                        .ParseArguments<
                            HelpOptions,
                            QueryLastOptions
                        >(args)
                        .MapResult(
                            (HelpOptions _) => RunHelp(),
                            (QueryLastOptions o) => QueryLast(host, o),
                            _ => Task.FromResult(1));
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

    private static IHostBuilder CreateHostBuilder(string[] args)
    {
        return Host.CreateDefaultBuilder(args)
            .UseSerilog()
            .ConfigureAppConfiguration((hostingContext, config) =>
            {
                config
                    //.SetBasePath(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location))
                    .AddJsonFile("appsettings.json", false, false)
                    .AddJsonFile($"appsettings.{hostingContext.HostingEnvironment.EnvironmentName}.json", true, false)
                    .AddJsonFile("appsettings.Local.json", true, false)
                    .AddEnvironmentVariables();
            })
            .UseStartup<Startup>();
    }
    
    private static Task<int> RunHelp()
    {
        return Task.FromResult(0);
    }

    private static async Task<int> QueryLast(IHost host, QueryLastOptions options)
    {
        var logger = host.Services.GetRequiredService<ILogger<Program>>();

        var query = host.Services.GetRequiredService<ILastMeasurementQuery>();
        var result = await query.Get(options.DevEui);

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
}
