using PDFComparisonUI.Models;

namespace PDFComparisonUI.Services;

public interface IJwtTokenService
{
    (string token, DateTime expiresAtUtc) GenerateToken(AppUser user);
}
