using Microsoft.EntityFrameworkCore;
using PDFComparisonUI.Models;

namespace PDFComparisonUI.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<AppUser> Users => Set<AppUser>();
}
