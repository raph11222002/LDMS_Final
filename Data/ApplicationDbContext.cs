using LDMS_Final.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LDMS_Final.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Product>              Products              { get; set; }
        public DbSet<ProductStock>         ProductStocks         { get; set; }
        public DbSet<ProductStockLog>      ProductStockLogs      { get; set; }
        public DbSet<ProductFavorite>      ProductFavorites      { get; set; }
        public DbSet<CartItem>             CartItems             { get; set; }
        public DbSet<Order>                Orders                { get; set; }
        public DbSet<OrderItem>            OrderItems            { get; set; }
        public DbSet<OrderStatusLog>       OrderStatusLogs       { get; set; }
        public DbSet<Notification>         Notifications         { get; set; }
        public DbSet<OrderRouteAssignment> OrderRouteAssignments { get; set; }
        public DbSet<OrderHubStop>         OrderHubStops         { get; set; }
        public DbSet<DriverVehicle> DriverVehicles { get; set; }
        public DbSet<UserActivityLog> UserActivityLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.HasCharSet("utf8mb4");

            // ── ApplicationUser ──────────────────────────────────────────
            builder.Entity<ApplicationUser>(e =>
            {
                e.Property(x => x.IsActive).HasColumnName("is_active").HasDefaultValue(true);
                e.Property(x => x.FullName).HasMaxLength(150);
                e.Property(x => x.ContactNumber).HasMaxLength(20);
                e.Property(x => x.CreatedByUserId).HasMaxLength(450);
                e.Property(x => x.AssignedHub).HasConversion<int?>();
            });

            // ── Notification ─────────────────────────────────────────────
            builder.Entity<Notification>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.Title).HasMaxLength(200);
                e.Property(x => x.Message).HasMaxLength(1000);
                e.Property(x => x.ActionUrl).HasMaxLength(500);
                e.Property(x => x.RecipientUserId).HasMaxLength(450);
                e.HasIndex(x => x.RecipientUserId);
            });

            // ── OrderRouteAssignment ─────────────────────────────────────
            builder.Entity<OrderRouteAssignment>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.Order)
                 .WithOne(o => o.RouteAssignment)
                 .HasForeignKey<OrderRouteAssignment>(x => x.OrderId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.AssignedByStaff)
                 .WithMany()
                 .HasForeignKey(x => x.AssignedByStaffId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            // ── OrderHubStop ─────────────────────────────────────────────
            builder.Entity<OrderHubStop>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasOne(x => x.RouteAssignment)
                 .WithMany(r => r.HubStops)
                 .HasForeignKey(x => x.OrderRouteAssignmentId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.AssignedDriver)
                 .WithMany()
                 .HasForeignKey(x => x.AssignedDriverId)
                 .OnDelete(DeleteBehavior.SetNull);

                e.Property(x => x.Hub).HasConversion<int>();
                e.Property(x => x.StopStatus).HasConversion<int>();
            });

            // ── OrderStatusLog ────────────────────────────────────────────
            builder.Entity<OrderStatusLog>(e =>
            {
                e.HasKey(x => x.Id);
                // IsVisibleToBuyer defaults false — only buyer-facing events are true
                e.Property(x => x.IsVisibleToBuyer).HasDefaultValue(false);
            });

            builder.Entity<UserActivityLog>(e =>
            {
                e.HasKey(x => x.Id);
                e.Property(x => x.UserId).HasMaxLength(450);
                e.Property(x => x.UserName).HasMaxLength(256);
                e.Property(x => x.FullName).HasMaxLength(150);
                e.Property(x => x.Role).HasMaxLength(50);
                e.Property(x => x.Action).HasMaxLength(100);
                e.Property(x => x.EntityType).HasMaxLength(50);
                e.Property(x => x.EntityId).HasMaxLength(100);
        
                // Indexes for fast filtering in the logs view
                e.HasIndex(x => x.UserId);
                e.HasIndex(x => x.CreatedAt);
                e.HasIndex(x => x.Action);
                e.HasIndex(x => x.Role);
            });
        }
    }
}
