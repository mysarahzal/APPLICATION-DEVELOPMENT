using AspnetCoreMvcFull.Models;
using Microsoft.EntityFrameworkCore;

namespace AspnetCoreMvcFull.Data
{
  public class KUTIPDbContext : DbContext
  {
    public KUTIPDbContext(DbContextOptions<KUTIPDbContext> options) : base(options)
    {
    }

    // DbSets for your models
    public DbSet<User> Users { get; set; }
    public DbSet<MissedPickup> MissedPickups { get; set; }
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<PickupReport> PickupReports { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<CollectionPoint> CollectionPoints { get; set; }
    public DbSet<Bin> Bins { get; set; }
    public DbSet<Truck> Trucks { get; set; }
    public DbSet<CollectionRecord> CollectionRecords { get; set; }

    // No custom configurations here, Entity Framework Core will use conventions
  }
}










