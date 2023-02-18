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
        // services.AddDbContext<WaterAlarmDbContext>(options =>
        //     options.UseSqlite(_configuration.GetConnectionString("WaterAlarmDB")));

        services.AddWaterAlarmCore(_configuration, typeof(Program).Assembly);
    }
}