using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.Services.Dtos;

namespace VeterinerKlinigi.Controllers.Api;

[ApiController]
[Route("api/appointments")]
[Authorize]
public class AppointmentsApiController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AppointmentsApiController(
        IAppointmentService appointmentService,
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        _appointmentService = appointmentService;
        _db = db;
        _userManager = userManager;
    }

    /// <summary>Kullanıcının kendi randevularını listeler (LINQ).</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<AppointmentDto>> GetMine()
    {
        var userId = _userManager.GetUserId(User)!;

        var query = _db.Appointments
            .Where(a => a.Pet!.OwnerId == userId)
            .OrderBy(a => a.AppointmentDate)
            .Select(a => new AppointmentDto
            {
                Id = a.Id,
                PetName = a.Pet!.Name,
                Species = a.Pet.Species,
                ServiceName = a.Service!.Name,
                VeterinarianName = a.Veterinarian!.FullName,
                AppointmentDate = a.AppointmentDate,
                DurationMinutes = a.DurationMinutes,
                Price = a.Price,
                Status = a.Status.ToString(),
                Notes = a.Notes
            });

        return Ok(query);
    }

    /// <summary>Yeni randevu oluşturur (durum: Bekliyor). İş kuralları servis katmanında.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(AppointmentResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(AppointmentResult), StatusCodes.Status400BadRequest)]
    public ActionResult<AppointmentResult> Create([FromBody] CreateAppointmentRequest request)
    {
        var userId = _userManager.GetUserId(User);

        // Yetkilendirme: üye yalnızca kendi hayvanı için randevu alabilir.
        var pet = _db.Pets.FirstOrDefault(p => p.Id == request.PetId);
        if (pet == null)
            return BadRequest(new AppointmentResult { Success = false, Message = "Evcil hayvan bulunamadı." });

        if (!User.IsInRole(DbSeeder.RoleAdmin) && !User.IsInRole(DbSeeder.RoleVeteriner)
            && pet.OwnerId != userId)
            return BadRequest(new AppointmentResult { Success = false, Message = "Bu hayvan için randevu alma yetkiniz yok." });

        var result = _appointmentService.CreateAppointment(request.PetId, request);
        if (!result.Success)
            return BadRequest(result);

        return CreatedAtAction(nameof(GetMine), new { }, result);
    }

    /// <summary>Bir veterinerin kendi randevularını listeler.</summary>
    [HttpGet("veterinarian")]
    [Authorize(Roles = DbSeeder.RoleVeteriner + "," + DbSeeder.RoleAdmin)]
    [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<AppointmentDto>> GetForVeterinarian()
    {
        var userId = _userManager.GetUserId(User)!;
        var user = _db.Users.Include(u => u.Veterinarian).FirstOrDefault(u => u.Id == userId);
        if (user?.VeterinarianId == null)
            return BadRequest(new { message = "Bu kullanıcı bir veterinere bağlı değil." });

        return Ok(_appointmentService.GetAppointmentsForVeterinarian(user.VeterinarianId.Value));
    }

    /// <summary>Bekleyen randevuyu onaylar (veteriner/admin).</summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = DbSeeder.RoleVeteriner + "," + DbSeeder.RoleAdmin)]
    [ProducesResponseType(typeof(AppointmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppointmentResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AppointmentResult> Approve(int id)
    {
        if (!_db.Appointments.Any(a => a.Id == id))
            return NotFound(new AppointmentResult { Success = false, Message = "Randevu bulunamadı." });

        var result = _appointmentService.Approve(id);
        return result.Success ? Ok(result) : BadRequest(result);
    }

    /// <summary>Bekleyen randevuyu reddeder (veteriner/admin).</summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = DbSeeder.RoleVeteriner + "," + DbSeeder.RoleAdmin)]
    [ProducesResponseType(typeof(AppointmentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(AppointmentResult), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<AppointmentResult> Reject(int id, [FromBody] RejectRequest request)
    {
        if (!_db.Appointments.Any(a => a.Id == id))
            return NotFound(new AppointmentResult { Success = false, Message = "Randevu bulunamadı." });

        var result = _appointmentService.Reject(id, request?.Reason ?? "Gerekçe belirtilmedi.");
        return result.Success ? Ok(result) : BadRequest(result);
    }
}

public class RejectRequest
{
    public string? Reason { get; set; }
}