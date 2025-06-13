using Microsoft.EntityFrameworkCore;
using AspnetCoreMvcFull.Models;
namespace AspnetCoreMvcFull.Data
{
  public class KUTIPDbContext : DbContext
  {
    public KUTIPDbContext(DbContextOptions<KUTIPDbContext> options) : base(options)
    {
    }
    public DbSet<User> Users { get; set; }
    public DbSet<MissedPickup> MissedPickups { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<PickupReport> PickupReports { get; set; }

  }
}
