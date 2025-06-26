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

    // FIXED: Change back to RoutePlans (plural) to match controller usage
    public DbSet<RoutePlan> RoutePlans { get; set; }

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

      // Use "RoutePlan" table name (singular)
      modelBuilder.Entity<RoutePlan>().ToTable("RoutePlan");

      // --- CollectionRecord Relationships ---

      modelBuilder.Entity<CollectionRecord>()
          .HasOne(cr => cr.CollectionPoint)
          .WithMany(cp => cp.CollectionRecords)
          .HasForeignKey(cr => cr.CollectionPointId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<CollectionRecord>()
          .HasOne(cr => cr.Bin)
          .WithMany(b => b.CollectionRecords)
          .HasForeignKey(cr => cr.BinId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<CollectionRecord>()
      .HasOne(cr => cr.Collector)
      .WithMany(u => u.CollectionRecords)   // <- use the real nav property
      .HasForeignKey(cr => cr.CollectorId)
      .OnDelete(DeleteBehavior.NoAction);



      modelBuilder.Entity<CollectionRecord>()
          .HasOne(cr => cr.Truck)
          .WithMany(t => t.CollectionRecords)
          .HasForeignKey(cr => cr.TruckId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<CollectionRecord>()
          .HasOne(cr => cr.Image)
          .WithOne(i => i.CollectionRecord)
          .HasForeignKey<Image>(i => i.CollectionRecordId)
          .OnDelete(DeleteBehavior.Cascade);

      // --- CollectionPoint Relationships ---

      modelBuilder.Entity<CollectionPoint>()
          .HasOne(cp => cp.Schedule)
          .WithMany(s => s.CollectionPoints)
          .HasForeignKey(cp => cp.ScheduleId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<CollectionPoint>()
          .HasOne(cp => cp.Bin)
          .WithMany(b => b.CollectionPoints)
          .HasForeignKey(cp => cp.BinId)
          .OnDelete(DeleteBehavior.NoAction);

      // --- Schedule Relationships ---

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

      // --- RouteBins Relationships ---

      modelBuilder.Entity<RouteBins>()
          .HasOne(rb => rb.RoutePlan)
          .WithMany(r => r.RouteBins)
          .HasForeignKey(rb => rb.RouteId)
          .OnDelete(DeleteBehavior.NoAction);

      modelBuilder.Entity<RouteBins>()
          .HasOne(rb => rb.Bin)
          .WithMany(b => b.RouteBins)
          .HasForeignKey(rb => rb.BinId)
          .OnDelete(DeleteBehavior.NoAction);
    }
  }
}
