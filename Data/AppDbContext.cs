using Microsoft.EntityFrameworkCore;
using PDFComparisonUI.Models;

namespace PDFComparisonUI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.UserName)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasIndex(u => u.Email)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .Property(u => u.UserName)
            .HasMaxLength(60);

        modelBuilder.Entity<AppUser>()
            .Property(u => u.Email)
            .HasMaxLength(120);
    }
}
