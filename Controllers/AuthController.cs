using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDFComparisonUI.Data;
using PDFComparisonUI.Dtos;
using PDFComparisonUI.Services;

namespace PDFComparisonUI.Controllers;

public class AuthController(AppDbContext db, IPasswordService passwordService, IJwtTokenService jwtTokenService) : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(x =>
            (x.UserName == request.UserNameOrEmail || x.Email == request.UserNameOrEmail) && x.IsActive);

        if (user is null || !passwordService.VerifyPassword(user.PasswordHash, request.Password))
        {
            ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
            return View(request);
        }

        var (token, expiresAtUtc) = jwtTokenService.GenerateToken(user);

        Response.Cookies.Append("AuthToken", token, new CookieOptions
        {
            HttpOnly = true,
            Secure = false,
            SameSite = SameSiteMode.Lax,
            Expires = expiresAtUtc
        });

        TempData["LoginSuccess"] = $"Logged in as {user.UserName} ({user.Role})";
        return RedirectToAction("Index", "Users");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("AuthToken");
        TempData["LogoutSuccess"] = "Session ended successfully.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost("api/auth/login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ApiLogin([FromBody] LoginRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(x =>
            (x.UserName == request.UserNameOrEmail || x.Email == request.UserNameOrEmail) && x.IsActive);

        if (user is null || !passwordService.VerifyPassword(user.PasswordHash, request.Password))
        {
            return Unauthorized(new { message = "Invalid credentials." });
        }

        var (token, expiresAtUtc) = jwtTokenService.GenerateToken(user);

        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            Role = user.Role.ToString(),
            UserName = user.UserName
        });
    }
}
