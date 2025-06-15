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
    public DbSet<Truck> Trucks { get; set; }
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
      base.OnModelCreating(modelBuilder);

      // Prevent multiple cascade delete paths for CollectionRecord relationships
      modelBuilder.Entity<CollectionRecord>()
          .HasOne(cr => cr.CollectionPoint)
          .WithMany(cp => cp.CollectionRecords)
          .HasForeignKey(cr => cr.CollectionPointId)
          .OnDelete(DeleteBehavior.Restrict);  // Disable cascade delete

      modelBuilder.Entity<CollectionRecord>()
          .HasOne(cr => cr.Bin)
          .WithMany(b => b.CollectionRecords)
          .HasForeignKey(cr => cr.BinId)
          .OnDelete(DeleteBehavior.Restrict);

      modelBuilder.Entity<CollectionRecord>()
          .HasOne(cr => cr.User)
          .WithMany(u => u.CollectionRecords)
          .HasForeignKey(cr => cr.UserId)
          .OnDelete(DeleteBehavior.Restrict);

      modelBuilder.Entity<CollectionRecord>()
          .HasOne(cr => cr.Truck)
          .WithMany(t => t.CollectionRecords)
          .HasForeignKey(cr => cr.TruckId)
          .OnDelete(DeleteBehavior.Restrict);
    }

    // No custom configurations here, Entity Framework Core will use conventions
  }
}
