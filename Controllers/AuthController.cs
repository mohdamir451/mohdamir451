using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PDFComparisonUI.Data;
using PDFComparisonUI.Dtos;
using PDFComparisonUI.Services;

namespace PDFComparisonUI.Controllers;

[AllowAnonymous]
public class AuthController(
    AppDbContext db,
    IPasswordService passwordService,
    IJwtTokenService jwtTokenService,
    ILogger<AuthController> logger) : Controller
{
    [HttpGet]
    public IActionResult Login()
    {
        return View();
    }

    [EnableRateLimiting("auth-login")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var user = await db.Users.FirstOrDefaultAsync(x =>
            (x.UserName == request.UserNameOrEmail || x.Email == request.UserNameOrEmail));

        if (user is null || !user.IsActive)
        {
            logger.LogWarning("Failed login for identifier {Identifier}: user missing or inactive", request.UserNameOrEmail);
            ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
            return View(request);
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc > DateTime.UtcNow)
        {
            logger.LogWarning("Blocked login for {UserName}: account locked until {LockoutEndUtc}", user.UserName, user.LockoutEndUtc);
            ModelState.AddModelError(string.Empty, $"Account is locked. Try again after {user.LockoutEndUtc:u}.");
            return View(request);
        }

        if (!passwordService.VerifyPassword(user.PasswordHash, request.Password))
        {
            user.FailedLoginAttempts += 1;
            if (user.FailedLoginAttempts >= PasswordPolicy.MaxFailedAttempts)
            {
                user.LockoutEndUtc = DateTime.UtcNow.Add(PasswordPolicy.LockoutDuration);
            }

            user.ModifiedBy = user.UserName;
            user.ModifiedDate = DateTime.UtcNow;
            await db.SaveChangesAsync();

            logger.LogWarning("Failed login for user {UserName}, attempt {FailedAttempts}", user.UserName, user.FailedLoginAttempts);
            ModelState.AddModelError(string.Empty, "Invalid username/email or password.");
            return View(request);
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.ModifiedBy = user.UserName;
        user.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.UserName),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            });

        logger.LogInformation("Successful login for user {UserName}", user.UserName);
        TempData["LoginSuccess"] = $"Logged in as {user.UserName} ({user.Role})";
        return RedirectToAction("Index", "Users");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TempData["LogoutSuccess"] = "Session ended successfully.";
        return RedirectToAction(nameof(Login));
    }

    [EnableRateLimiting("auth-login")]
    [HttpPost("api/auth/login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status423Locked)]
    public async Task<IActionResult> ApiLogin([FromBody] LoginRequest request)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var user = await db.Users.FirstOrDefaultAsync(x =>
            (x.UserName == request.UserNameOrEmail || x.Email == request.UserNameOrEmail) && x.IsActive);

        if (user is null)
        {
            logger.LogWarning("API failed login for identifier {Identifier}: not found/active", request.UserNameOrEmail);
            return Unauthorized(new ProblemDetails { Title = "Invalid credentials.", Status = StatusCodes.Status401Unauthorized });
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc > DateTime.UtcNow)
        {
            return StatusCode(StatusCodes.Status423Locked, new ProblemDetails
            {
                Title = "Account locked.",
                Detail = $"Try again after {user.LockoutEndUtc:u}.",
                Status = StatusCodes.Status423Locked
            });
        }

        if (!passwordService.VerifyPassword(user.PasswordHash, request.Password))
        {
            user.FailedLoginAttempts += 1;
            if (user.FailedLoginAttempts >= PasswordPolicy.MaxFailedAttempts)
            {
                user.LockoutEndUtc = DateTime.UtcNow.Add(PasswordPolicy.LockoutDuration);
            }

            user.ModifiedBy = user.UserName;
            user.ModifiedDate = DateTime.UtcNow;
            await db.SaveChangesAsync();
            return Unauthorized(new ProblemDetails { Title = "Invalid credentials.", Status = StatusCodes.Status401Unauthorized });
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndUtc = null;
        user.ModifiedBy = user.UserName;
        user.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var (token, expiresAtUtc) = jwtTokenService.GenerateToken(user);

        logger.LogInformation("API successful login for user {UserName}", user.UserName);
        return Ok(new LoginResponse
        {
            Token = token,
            ExpiresAtUtc = expiresAtUtc,
            Role = user.Role.ToString(),
            UserName = user.UserName
        });
    }
}
