using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using Core.Commands;
using Core.Queries;
using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Core;

[ExcludeFromCodeCoverage]
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddWaterAlarmCore(this IServiceCollection services, IConfiguration configuration,
        Assembly assembly)
    {
        services.AddScoped<ILastMeasurementQuery, LastMeasurementQuery>();

        services.AddMediatR(o => { o.RegisterServicesFromAssembly(typeof(CreateAccountCommandHandler).Assembly); });

        services.Configure<MeasurementInfluxOptions>(configuration.GetSection(MeasurementInfluxOptions.Position));
        services.AddScoped<IMeasurementRepository, MeasurementRepository>();

        services.AddSingleton<IVersionInfo, VersionInfo>(_ => new VersionInfo(assembly));

        services.AddDbContext<WaterAlarmDbContext>(o => o.UseSqlite(configuration.GetConnectionString("WaterAlarmDb")));

        return services;
    }
}