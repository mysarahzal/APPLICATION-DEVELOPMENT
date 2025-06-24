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
    public DbSet<Client> Clients { get; set; }
    public DbSet<Bin> Bins { get; set; }

    public DbSet<RouteBins> RouteBins { get; set; }

    // Other models
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<CollectionPoint> CollectionPoints { get; set; }
    public DbSet<CollectionRecord> CollectionRecords { get; set; }
    public DbSet<Image> Images { get; set; }
    public DbSet<MissedPickup> MissedPickups { get; set; }
    public DbSet<PickupReport> PickupReports { get; set; }
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<Road> Roads { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<Truck> Trucks { get; set; }
    public DbSet<BinReport> BinReports { get; set; }
  }
}


