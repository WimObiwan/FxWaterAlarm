using Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core;

public static class HostExternsions
{
    public static IHost MigrateWaterAlarmDb(this IHost host)
    {
        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WaterAlarmDbContext>();
        db.Database.Migrate();
        return host;
    }
}