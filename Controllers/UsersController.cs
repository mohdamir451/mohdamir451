using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PDFComparisonUI.Data;
using PDFComparisonUI.Dtos;
using PDFComparisonUI.Models;
using PDFComparisonUI.Services;

namespace PDFComparisonUI.Controllers;

[Authorize(Policy = "ReviewerOrAdmin")]
public class UsersController(AppDbContext db, IPasswordService passwordService) : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var users = await db.Users.OrderByDescending(x => x.CreatedDate).ToListAsync();
        return View(new UsersDashboardViewModel
        {
            Users = users,
            NewUser = new UserUpsertRequest { IsActive = true }
        });
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Create()
    {
        return RedirectToAction(nameof(Index), new { section = "create-user" });
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(UserUpsertRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Password))
        {
            ModelState.AddModelError(nameof(request.Password), "Password is required.");
        }

        if (!ModelState.IsValid)
        {
            var users = await db.Users.OrderByDescending(x => x.CreatedDate).ToListAsync();
            return View(nameof(Index), new UsersDashboardViewModel
            {
                Users = users,
                NewUser = request
            });
        }

        var now = DateTime.UtcNow;

        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = request.UserName,
            Email = request.Email,
            PasswordHash = passwordService.HashPassword(request.Password!),
            Role = request.Role,
            IsActive = request.IsActive,
            CreatedBy = "Admin",
            CreatedDate = now,
            ModifiedBy = "Admin",
            ModifiedDate = now
        };

        db.Users.Add(user);
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { section = "users" });
    }

    [AllowAnonymous]
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

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Guid id, UserUpsertRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id);
        if (user is null)
        {
            return NotFound();
        }

        user.UserName = request.UserName;
        user.Email = request.Email;
        user.Role = request.Role;
        user.IsActive = request.IsActive;

        if (!string.IsNullOrWhiteSpace(request.Password))
        {
            user.PasswordHash = passwordService.HashPassword(request.Password);
        }

        user.ModifiedBy = "Admin";
        user.ModifiedDate = DateTime.UtcNow;

        await db.SaveChangesAsync();
        return RedirectToAction(nameof(Index), new { section = "users" });
    }

    [AllowAnonymous]
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
        user.ModifiedBy = "Admin";
        user.ModifiedDate = DateTime.UtcNow;
        await db.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { section = "users" });
    }
}
