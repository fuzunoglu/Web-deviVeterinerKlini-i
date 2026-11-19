using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Controllers;

[Authorize(Roles = DbSeeder.RoleAdmin)]
public class ClinicsController : Controller
{
    private readonly ApplicationDbContext _db;

    public ClinicsController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
        => View(await _db.Clinics.OrderBy(c => c.Name).ToListAsync());

    public async Task<IActionResult> Details(int id)
    {
        var clinic = await _db.Clinics
            .Include(c => c.Services)
            .Include(c => c.Veterinarians)
            .FirstOrDefaultAsync(c => c.Id == id);
        if (clinic == null) return NotFound();
        return View(clinic);
    }

    public IActionResult Create() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Clinic clinic)
    {
        if (!ModelState.IsValid) return View(clinic);
        _db.Clinics.Add(clinic);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Klinik oluşturuldu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var clinic = await _db.Clinics.FindAsync(id);
        if (clinic == null) return NotFound();
        return View(clinic);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Clinic clinic)
    {
        if (id != clinic.Id) return NotFound();
        if (!ModelState.IsValid) return View(clinic);
        _db.Update(clinic);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Klinik güncellendi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var clinic = await _db.Clinics.FindAsync(id);
        if (clinic == null) return NotFound();
        return View(clinic);
    }

    [HttpPost, ActionName("Delete"), ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var clinic = await _db.Clinics.FindAsync(id);
        if (clinic == null) return NotFound();
        _db.Clinics.Remove(clinic);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Klinik silindi.";
        return RedirectToAction(nameof(Index));
    }
}