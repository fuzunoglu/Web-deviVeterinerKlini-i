using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.Services.Dtos;

namespace VeterinerKlinigi.Controllers;

[Authorize]
public class AppointmentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly IAppointmentService _appointmentService;
    private readonly IVeterinarianService _vetService;
    private readonly UserManager<ApplicationUser> _userManager;

    public AppointmentsController(
        ApplicationDbContext db,
        IAppointmentService appointmentService,
        IVeterinarianService vetService,
        UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _appointmentService = appointmentService;
        _vetService = vetService;
        _userManager = userManager;
    }

    private string UserId => _userManager.GetUserId(User)!;
    private bool IsPrivileged => User.IsInRole(DbSeeder.RoleAdmin) || User.IsInRole(DbSeeder.RoleVeteriner);

    // ---- Randevu listesi ----
    public async Task<IActionResult> Index()
    {
        var query = _db.Appointments
            .Include(a => a.Pet).Include(a => a.Service)
            .Include(a => a.Veterinarian).Include(a => a.ExaminationNote)
            .AsQueryable();

        if (User.IsInRole(DbSeeder.RoleUye))
            query = query.Where(a => a.Pet!.OwnerId == UserId);
        else if (User.IsInRole(DbSeeder.RoleVeteriner))
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            if (user?.VeterinarianId != null)
                query = query.Where(a => a.VeterinarianId == user.VeterinarianId);
        }

        var list = await query.OrderBy(a => a.AppointmentDate).ToListAsync();
        return View(list);
    }

    // ---- Randevu oluşturma (üye) ----
    [Authorize(Roles = DbSeeder.RoleUye + "," + DbSeeder.RoleAdmin)]
    public async Task<IActionResult> Create()
    {
        ViewBag.Pets = await GetAccessiblePetsAsync();
        ViewBag.Services = await _db.Services.OrderBy(s => s.Name).ToListAsync();
        ViewBag.Veterinarians = await _db.Veterinarians.Where(v => v.IsActive).OrderBy(v => v.FullName).ToListAsync();
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = DbSeeder.RoleUye + "," + DbSeeder.RoleAdmin)]
    public async Task<IActionResult> Create(int petId, int serviceId, int veterinarianId, DateTime appointmentDate, string? notes)
    {
        var result = _appointmentService.CreateAppointment(petId, new CreateAppointmentRequest
        {
            PetId = petId,
            ServiceId = serviceId,
            VeterinarianId = veterinarianId,
            AppointmentDate = appointmentDate,
            Notes = notes
        });

        if (!result.Success)
        {
            TempData["Error"] = result.Message;
            ViewBag.Pets = await GetAccessiblePetsAsync();
            ViewBag.Services = await _db.Services.OrderBy(s => s.Name).ToListAsync();
            ViewBag.Veterinarians = await _db.Veterinarians.Where(v => v.IsActive).OrderBy(v => v.FullName).ToListAsync();
            return View();
        }

        TempData["Success"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // ---- Randevu detayı ----
    public async Task<IActionResult> Details(int id)
    {
        var a = await _db.Appointments
            .Include(a => a.Pet).ThenInclude(p => p!.Owner)
            .Include(a => a.Service).Include(a => a.Veterinarian)
            .Include(a => a.ExaminationNote)
            .FirstOrDefaultAsync(a => a.Id == id);
        if (a == null) return NotFound();
        if (User.IsInRole(DbSeeder.RoleUye) && a.Pet!.OwnerId != UserId) return Forbid();
        if (User.IsInRole(DbSeeder.RoleVeteriner))
        {
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == UserId);
            if (user?.VeterinarianId != a.VeterinarianId) return Forbid();
        }
        return View(a);
    }

    // ---- Onay / Red (veteriner, admin) ----
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = DbSeeder.RoleVeteriner + "," + DbSeeder.RoleAdmin)]
    public async Task<IActionResult> Approve(int id)
    {
        var result = _appointmentService.Approve(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = DbSeeder.RoleVeteriner + "," + DbSeeder.RoleAdmin)]
    public async Task<IActionResult> Reject(int id, string reason)
    {
        var result = _appointmentService.Reject(id, reason);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = DbSeeder.RoleVeteriner + "," + DbSeeder.RoleAdmin)]
    public async Task<IActionResult> Complete(int id)
    {
        var result = _appointmentService.Complete(id);
        TempData[result.Success ? "Success" : "Error"] = result.Message;
        return RedirectToAction(nameof(Index));
    }

    // ---- Muayene notu ----
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = DbSeeder.RoleVeteriner + "," + DbSeeder.RoleAdmin)]
    public async Task<IActionResult> AddNote(int appointmentId, string content)
    {
        var appt = await _db.Appointments.FirstOrDefaultAsync(a => a.Id == appointmentId);
        if (appt == null) return NotFound();
        if (appt.Status != AppointmentStatus.Tamamlandi && appt.Status != AppointmentStatus.Onaylandi)
        {
            TempData["Error"] = "Yalnızca onaylanmış/tamamlanmış randevulara not eklenebilir.";
            return RedirectToAction(nameof(Details), new { id = appointmentId });
        }
        var note = new ExaminationNote { AppointmentId = appointmentId, Content = content, CreatedAt = DateTime.UtcNow };
        _db.ExaminationNotes.Add(note);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Muayene notu eklendi.";
        return RedirectToAction(nameof(Details), new { id = appointmentId });
    }

    // ---- JSON: müsait saatler (AJAX için) ----
    [HttpGet]
    public JsonResult AvailableSlots(int veterinarianId, int serviceId, DateTime date)
    {
        var slots = _appointmentService.GetAvailableSlots(veterinarianId, serviceId, date);
        return Json(slots.Select(s => new { start = s.Start.ToString("yyyy-MM-ddTHH:mm"), label = s.Start.ToString("HH:mm") }));
    }

    // ---- JSON: hizmete göre uygun veterinerler ----
    [HttpGet]
    public JsonResult AvailableVeterinarians(int serviceId, DateTime date)
    {
        var vets = _vetService.GetByServiceAndDate(serviceId, date);
        return Json(vets.Select(v => new { id = v.Id, name = v.FullName }));
    }

    private async Task<List<Pet>> GetAccessiblePetsAsync()
    {
        var query = _db.Pets.AsQueryable();
        if (User.IsInRole(DbSeeder.RoleUye))
            query = query.Where(p => p.OwnerId == UserId);
        return await query.OrderBy(p => p.Name).ToListAsync();
    }
}