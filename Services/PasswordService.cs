using Microsoft.AspNetCore.Identity;

namespace PDFComparisonUI.Services;

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<object> _passwordHasher = new();
    private static readonly object UserContext = new();

    public string HashPassword(string password)
    {
        return _passwordHasher.HashPassword(UserContext, password);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _passwordHasher.VerifyHashedPassword(UserContext, hashedPassword, providedPassword);
        return result == PasswordVerificationResult.Success || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
