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
    public DbSet<RoutePlan> RoutePlans { get; set; }  // DbSet name (plural)

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // IMPORTANT: Configure RoutePlan to use the existing "RoutePlan" table name (singular)
      modelBuilder.Entity<RoutePlan>().ToTable("RoutePlan");

      // Configure CollectionRecord relationships
      modelBuilder.Entity<CollectionRecord>()
          .HasOne(cr => cr.CollectionPoint)
          .WithMany(cp => cp.CollectionRecords)
          .HasForeignKey(cr => cr.CollectionPointId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<CollectionRecord>()
          .HasOne(cr => cr.Bin)
          .WithMany()
          .HasForeignKey(cr => cr.BinId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<CollectionRecord>()
          .HasOne(cr => cr.User)
          .WithMany()
          .HasForeignKey(cr => cr.UserId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<CollectionRecord>()
          .HasOne(cr => cr.Truck)
          .WithMany(t => t.CollectionRecords)
          .HasForeignKey(cr => cr.TruckId)
          .OnDelete(DeleteBehavior.NoAction);

      // Configure CollectionPoint relationships
      modelBuilder.Entity<CollectionPoint>()
          .HasOne(cp => cp.Schedule)
          .WithMany(s => s.CollectionPoints)
          .HasForeignKey(cp => cp.ScheduleId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<CollectionPoint>()
          .HasOne(cp => cp.Bin)
          .WithMany()
          .HasForeignKey(cp => cp.BinId)
          .OnDelete(DeleteBehavior.NoAction);

      // Configure Schedule relationships
      modelBuilder.Entity<Schedule>()
          .HasOne(s => s.Truck)
          .WithMany()
          .HasForeignKey(s => s.TruckId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<Schedule>()
          .HasOne(s => s.Route)
          .WithMany()
          .HasForeignKey(s => s.RouteId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<Schedule>()
          .HasOne(s => s.Collector)
          .WithMany()
          .HasForeignKey(s => s.CollectorId)
          .OnDelete(DeleteBehavior.NoAction);

      // Configure RouteBins relationships
      modelBuilder.Entity<RouteBins>()
          .HasOne(rb => rb.RoutePlan)
          .WithMany(r => r.RouteBins)
          .HasForeignKey(rb => rb.RouteId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<RouteBins>()
          .HasOne(rb => rb.Bin)
          .WithMany()
          .HasForeignKey(rb => rb.BinId)
          .OnDelete(DeleteBehavior.NoAction);
    }
  }
}
