using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Controllers;

[Authorize(Roles = DbSeeder.RoleAdmin)]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index()
    {
        var stats = new
        {
            ClinicCount = await _db.Clinics.CountAsync(),
            ServiceCount = await _db.Services.CountAsync(),
            VeterinarianCount = await _db.Veterinarians.CountAsync(),
            PetCount = await _db.Pets.CountAsync(),
            AppointmentCount = await _db.Appointments.CountAsync(),
            PendingCount = await _db.Appointments.CountAsync(a => a.Status == Models.AppointmentStatus.Bekliyor),
            UserCount = await _db.Users.CountAsync()
        };
        ViewBag.Stats = stats;
        return View();
    }

    public async Task<IActionResult> Users()
    {
        var users = await _db.Users.Include(u => u.Veterinarian).OrderBy(u => u.Email).ToListAsync();
        var roles = new Dictionary<string, IList<string>>();
        foreach (var u in users)
            roles[u.Id] = await _userManager.GetRolesAsync(u);
        ViewBag.Roles = roles;
        return View(users);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AssignRole(string userId, string role)
    {
        var user = await _userManager.FindByIdAsync(userId);
        if (user == null) return NotFound();
        if (!new[] { DbSeeder.RoleAdmin, DbSeeder.RoleVeteriner, DbSeeder.RoleUye }.Contains(role))
        {
            TempData["Error"] = "Geçersiz rol.";
            return RedirectToAction(nameof(Users));
        }
        var currentRoles = await _userManager.GetRolesAsync(user);
        await _userManager.RemoveFromRolesAsync(user, currentRoles);
        await _userManager.AddToRoleAsync(user, role);
        TempData["Success"] = $"{user.Email} kullanıcısına {role} rolü atandı.";
        return RedirectToAction(nameof(Users));
    }
}