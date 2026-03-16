using System.Text.RegularExpressions;

namespace PDFComparisonUI.Services;

public static partial class PasswordPolicy
{
    public const int MaxFailedAttempts = 5;
    public static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    public const string RequirementsMessage =
        "Password must be at least 8 characters and include upper, lower, number, and special character.";

    public static bool IsStrong(string password)
    {
        if (password.Length < 8)
        {
            return false;
        }

        return HasUpper().IsMatch(password)
               && HasLower().IsMatch(password)
               && HasNumber().IsMatch(password)
               && HasSpecial().IsMatch(password);
    }

    [GeneratedRegex("[A-Z]")]
    private static partial Regex HasUpper();

    [GeneratedRegex("[a-z]")]
    private static partial Regex HasLower();

    [GeneratedRegex("[0-9]")]
    private static partial Regex HasNumber();

    [GeneratedRegex("[^a-zA-Z0-9]")]
    private static partial Regex HasSpecial();
}
