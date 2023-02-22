using Microsoft.EntityFrameworkCore;

namespace Core;

/// <summary>
///     The entity framework context
/// </summary>
public class WaterAlarmDbContext : DbContext
{
    public WaterAlarmDbContext(DbContextOptions<WaterAlarmDbContext> options)
        : base(options)
    {
    }
}