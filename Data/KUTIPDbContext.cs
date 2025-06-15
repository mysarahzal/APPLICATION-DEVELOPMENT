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
    public DbSet<Alert> Alerts { get; set; }
    public DbSet<Bin> Bins { get; set; }
    //binreport.cs
    public DbSet<CollectionPoint> CollectionPoints { get; set; }
    public DbSet<CollectionRecord> CollectionRecords { get; set; }
    //detectedbindetails.cs
    public DbSet<Image> Images { get; set; }
    public DbSet<MissedPickup> MissedPickups { get; set; }
    public DbSet<PickupReport> PickupReports { get; set; }
    //road.cs
    public DbSet<Schedule> Schedules { get; set; }
    public DbSet<PickupReport> PickupReports { get; set; }

    // No custom configurations here, Entity Framework Core will use conventions
  }
}
