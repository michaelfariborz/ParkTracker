using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ParkTracker.Data.Models;

namespace ParkTracker.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<ApplicationUser>(options)
{
    public DbSet<Park> Parks => Set<Park>();
    public DbSet<Visit> Visits => Set<Visit>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Visit>(e =>
        {
            e.HasOne(v => v.Park)
                .WithMany(p => p.Visits)
                .HasForeignKey(v => v.ParkId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(v => v.User)
                .WithMany(u => u.Visits)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Property(v => v.CreatedAt)
                .HasDefaultValueSql("GETUTCDATE()");
        });

        builder.Entity<Park>(e =>
        {
            e.HasIndex(p => p.Name).IsUnique();
        });
    }
}
