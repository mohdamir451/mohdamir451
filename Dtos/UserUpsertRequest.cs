using System.ComponentModel.DataAnnotations;
using PDFComparisonUI.Models;
using PDFComparisonUI.Services;

namespace PDFComparisonUI.Dtos;

public class UserUpsertRequest : IValidatableObject
{
    [Required]
    [StringLength(60, MinimumLength = 3)]
    [RegularExpression("^[a-zA-Z0-9._-]+$", ErrorMessage = "Username can include letters, numbers, dots, underscores, and dashes only.")]
    public string UserName { get; set; } = string.Empty;

    [Required]
    [EmailAddress]
    [StringLength(120)]
    public string Email { get; set; } = string.Empty;

    [StringLength(128, MinimumLength = 8)]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Required]
    public UserRole Role { get; set; } = UserRole.User;

    public bool IsActive { get; set; } = true;

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var action = validationContext.Items.TryGetValue("Action", out var actionValue)
            ? actionValue?.ToString()
            : null;

        if (string.Equals(action, "Create", StringComparison.OrdinalIgnoreCase) && string.IsNullOrWhiteSpace(Password))
        {
            yield return new ValidationResult("Password is required for new users.", new[] { nameof(Password) });
        }

        if (!string.IsNullOrWhiteSpace(Password) && !PasswordPolicy.IsStrong(Password))
        {
            yield return new ValidationResult(PasswordPolicy.RequirementsMessage, new[] { nameof(Password) });
        }
    }
}
