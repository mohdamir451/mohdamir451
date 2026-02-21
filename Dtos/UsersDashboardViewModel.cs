using PDFComparisonUI.Models;

namespace PDFComparisonUI.Dtos;

public class UsersDashboardViewModel
{
    public IReadOnlyCollection<AppUser> Users { get; set; } = [];
    public UserUpsertRequest NewUser { get; set; } = new();
}
