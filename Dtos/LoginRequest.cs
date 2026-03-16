using System.ComponentModel.DataAnnotations;

namespace PDFComparisonUI.Dtos;

public class LoginRequest
{
    [Required]
    [StringLength(120, MinimumLength = 3)]
    public string UserNameOrEmail { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
