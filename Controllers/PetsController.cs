using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Controllers;

[Authorize]
public class PetsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public PetsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private string UserId => _userManager.GetUserId(User)!;
    private bool IsPrivileged => User.IsInRole(DbSeeder.RoleAdmin) || User.IsInRole(DbSeeder.RoleVeteriner);

    public async Task<IActionResult> Index()
    {
        var query = _db.Pets.Include(p => p.Owner).AsQueryable();
        if (!IsPrivileged)
            query = query.Where(p => p.OwnerId == UserId);
        return View(await query.OrderBy(p => p.Name).ToListAsync());
    }

    public async Task<IActionResult> Details(int id)
    {
        var pet = await _db.Pets
            .Include(p => p.VaccineRecords)
            .Include(p => p.Appointments).ThenInclude(a => a.Service)
            .Include(p => p.Appointments).ThenInclude(a => a.Veterinarian)
            .FirstOrDefaultAsync(p => p.Id == id);
        if (pet == null) return NotFound();
        if (!IsPrivileged && pet.OwnerId != UserId) return Forbid();
        return View(pet);
    }

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Pet pet)
    {
        pet.OwnerId = UserId;
        if (!ModelState.IsValid) return View(pet);
        _db.Pets.Add(pet);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Evcil hayvan kaydedildi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var pet = await _db.Pets.FindAsync(id);
        if (pet == null) return NotFound();
        if (!IsPrivileged && pet.OwnerId != UserId) return Forbid();
        return View(pet);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Pet pet)
    {
        if (id != pet.Id) return NotFound();
        var existing = await _db.Pets.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);
        if (existing == null) return NotFound();
        if (!IsPrivileged && existing.OwnerId != UserId) return Forbid();

        pet.OwnerId = existing.OwnerId; // sahipliği değiştirilemez.
        if (!ModelState.IsValid) return View(pet);
        _db.Update(pet);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Evcil hayvan güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var pet = await _db.Pets.Include(p => p.Owner).FirstOrDefaultAsync(p => p.Id == id);
        if (pet == null) return NotFound();
        if (!IsPrivileged && pet.OwnerId != UserId) return Forbid();
        return View(pet);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var pet = await _db.Pets.FindAsync(id);
        if (pet == null) return NotFound();
        if (!IsPrivileged && pet.OwnerId != UserId) return Forbid();
        _db.Pets.Remove(pet);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Evcil hayvan silindi.";
        return RedirectToAction(nameof(Index));
    }
}