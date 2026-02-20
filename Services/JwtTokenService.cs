using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using PDFComparisonUI.Models;

namespace PDFComparisonUI.Services;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    public (string token, DateTime expiresAtUtc) GenerateToken(AppUser user)
    {
        var key = configuration["Jwt:Key"] ?? "SuperSecureDemoKeyForJwtTokenGeneration123!";
        var issuer = configuration["Jwt:Issuer"] ?? "PDFComparisonUI";
        var audience = configuration["Jwt:Audience"] ?? "PDFComparisonUsers";
        var expiryMinutes = int.TryParse(configuration["Jwt:ExpiryMinutes"], out var configured) ? configured : 30;

        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(signingKey, SecurityAlgorithms.HmacSha256);

        var tokenDescriptor = new JwtSecurityToken(
            issuer,
            audience,
            claims,
            expires: expires,
            signingCredentials: credentials);

        var token = new JwtSecurityTokenHandler().WriteToken(tokenDescriptor);
        return (token, expires);
    }
}
