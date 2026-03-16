using PDFComparisonUI.Models;

namespace PDFComparisonUI.Dtos;

public class UserUpsertRequest
{
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Password { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsActive { get; set; } = true;
}
