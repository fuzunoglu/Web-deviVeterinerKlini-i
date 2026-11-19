using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        ViewBag.ClinicCount = await _db.Clinics.CountAsync();
        ViewBag.ServiceCount = await _db.Services.CountAsync();
        ViewBag.VeterinarianCount = await _db.Veterinarians.CountAsync();
        ViewBag.UpcomingVaccines = await _db.VaccineRecords
            .CountAsync(v => v.NextDueDate != null && v.NextDueDate <= DateTime.Today.AddDays(30));
        return View();
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View();
}