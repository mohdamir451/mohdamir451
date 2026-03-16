using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDFComparisonUI.Data;
using PDFComparisonUI.Dtos;
using PDFComparisonUI.Models;
using PDFComparisonUI.Services;

namespace PDFComparisonUI.Controllers;

[Authorize(Policy = "ReviewerOrAdmin")]
public class UsersController(AppDbContext db, IPasswordService passwordService, ILogger<UsersController> logger) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await db.Users.OrderByDescending(x => x.CreatedDate).ToListAsync();
        return View(users);
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public IActionResult Create()
    {
        return View(new UserUpsertRequest { IsActive = true });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserUpsertRequest request)
    {
        var validationContext = new ValidationContext(request, HttpContext.RequestServices, new Dictionary<object, object?>
        {
            ["Action"] = "Create"
        });

        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(request, validationContext, results, true))
        {
            foreach (var validationResult in results)
            {
                foreach (var memberName in validationResult.MemberNames.DefaultIfEmpty(string.Empty))
                {
                    ModelState.AddModelError(memberName, validationResult.ErrorMessage ?? "Validation failed.");
                }
            }
        }

        if (await db.Users.AnyAsync(x => x.UserName == request.UserName))
        {
            ModelState.AddModelError(nameof(request.UserName), "Username is already in use.");
        }

        if (await db.Users.AnyAsync(x => x.Email == request.Email))
        {
            ModelState.AddModelError(nameof(request.Email), "Email is already in use.");
        }

        if (!ModelState.IsValid)
        {
            return View(request);
        }

        var now = DateTime.UtcNow;
        var actor = User.Identity?.Name ?? "Admin";

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = passwordService.HashPassword(request.Password!),
            Role = request.Role,
            IsActive = request.IsActive,
            CreatedBy = actor,
            CreatedDate = now,
            ModifiedBy = actor,
            ModifiedDate = now
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();
        logger.LogInformation("User {CreatedUser} created by {Actor}", user.UserName, actor);

        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpGet]
    public async Task<IActionResult> Edit(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        return View(new UserUpsertRequest
        {
            UserName = user.UserName,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive
        });
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UserUpsertRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        if (await db.Users.AnyAsync(x => x.Id != id && x.UserName == request.UserName))
        {
            ModelState.AddModelError(nameof(request.UserName), "Username is already in use.");
        }

        if (await db.Users.AnyAsync(x => x.Id != id && x.Email == request.Email))
        {
            ModelState.AddModelError(nameof(request.Email), "Email is already in use.");
        }

        if (!string.IsNullOrWhiteSpace(request.Password) && !PasswordPolicy.IsStrong(request.Password))
        {
            ModelState.AddModelError(nameof(request.Password), PasswordPolicy.RequirementsMessage);
        }

        if (!ModelState.IsValid)
        {
            return View(request);
        }

        user.UserName = request.UserName;
        user.Email = request.Email;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = passwordService.HashPassword(request.Password);
        }

        var actor = User.Identity?.Name ?? "Admin";
        user.ModifiedBy = actor;
        user.ModifiedDate = DateTime.UtcNow;

        await db.SaveChangesAsync();
        logger.LogInformation("User {UserName} updated by {Actor}", user.UserName, actor);
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Policy = "AdminOnly")]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStatus(Guid id)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        user.IsActive = !user.IsActive;
        user.ModifiedBy = User.Identity?.Name ?? "Admin";
        user.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();

        logger.LogInformation("User {UserName} status changed to {IsActive} by {Actor}", user.UserName, user.IsActive, user.ModifiedBy);
        return RedirectToAction(nameof(Index));
    }
}
