using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InventoryDashboard.Models;

namespace InventoryDashboard.Data
{
    /// <summary>
    /// Main EF Core database context for the Inventory Dashboard application.
    /// Inherits from IdentityDbContext to include ASP.NET Identity tables.
    /// </summary>
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        /// <summary>Inventory items table.</summary>
        public DbSet<Item> Items { get; set; }

        /// <summary>Audit log table for tracking all item mutations.</summary>
        public DbSet<AuditLog> AuditLogs { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Item unique serial number constraint
            builder.Entity<Item>()
                .HasIndex(i => i.SerialNumber)
                .IsUnique();

            // Item → User FK (nullable — don't cascade delete users)
            builder.Entity<Item>()
                .HasOne(i => i.AddedByUser)
                .WithMany(u => u.Items)
                .HasForeignKey(i => i.AddedByUserID)
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
