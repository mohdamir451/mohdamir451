using PDFComparisonUI.Models;
using PDFComparisonUI.Services;

namespace PDFComparisonUI.Data;

public static class DbSeeder
{
    public static void Seed(AppDbContext db, IPasswordService passwordService)
    {
        if (db.Users.Any())
        {
            return;
        }

        var now = DateTime.UtcNow;

        var admin = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "admin",
            Email = "admin@pdfcompare.local",
            Role = UserRole.Admin,
            IsActive = true,
            PasswordHash = passwordService.HashPassword("Admin@123"),
            CreatedBy = "System",
            CreatedDate = now,
            ModifiedBy = "System",
            ModifiedDate = now
        };

        var reviewer = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = "reviewer",
            Email = "reviewer@pdfcompare.local",
            Role = UserRole.Reviewer,
            IsActive = true,
            PasswordHash = passwordService.HashPassword("Reviewer@123"),
            CreatedBy = "System",
            CreatedDate = now,
            ModifiedBy = "System",
            ModifiedDate = now
        };

        db.Users.AddRange(admin, reviewer);
        db.SaveChanges();
    }
}
