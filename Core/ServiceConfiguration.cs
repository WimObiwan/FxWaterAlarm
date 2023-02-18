using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Core.Queries;
using Core.Repository;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

[ExcludeFromCodeCoverage]
public static class ServiceConfiguration
{
    public static IServiceCollection AddWaterAlarmCore(this IServiceCollection services, IConfiguration configuration,
        Assembly assembly)
    {
        services.AddScoped<ILastMeasurementQuery, LastMeasurementQuery>();

        services.Configure<MeasurementInfluxOptions>(configuration.GetSection(MeasurementInfluxOptions.Position));
        services.AddScoped<IMeasurementRepository, MeasurementRepository>();

        services.AddSingleton<IVersionInfo, VersionInfo>(_ => new VersionInfo(assembly));

        return services;
    }
}