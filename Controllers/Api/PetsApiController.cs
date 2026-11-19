using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;

namespace VeterinerKlinigi.Controllers.Api;

[ApiController]
[Route("api/pets")]
[Authorize]
public class PetsApiController : ControllerBase
{
    private readonly IPetService _petService;
    private readonly UserManager<ApplicationUser> _userManager;

    public PetsApiController(IPetService petService, UserManager<ApplicationUser> userManager)
    {
        _petService = petService;
        _userManager = userManager;
    }

    /// <summary>Oturum açmış kullanıcının evcil hayvanlarını listeler.</summary>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(IEnumerable<PetDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<PetDto>> GetMine()
    {
        var userId = _userManager.GetUserId(User)!;
        return Ok(_petService.GetByOwner(userId));
    }

    /// <summary>Bir evcil hayvanın aşı geçmişini ve yaklaşan aşılarını getirir (LINQ).</summary>
    [HttpGet("{id:int}/vaccines")]
    [ProducesResponseType(typeof(PetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<PetDto> GetVaccines(int id)
    {
        var userId = _userManager.GetUserId(User);
        var pet = _petService.GetById(id);

        if (pet == null)
            return NotFound(new { message = "Evcil hayvan bulunamadı." });

        // Yetkilendirme: bir üye yalnızca kendi hayvanını görebilir (admin ve veteriner hariç).
        if (!User.IsInRole(DbSeeder.RoleAdmin) && !User.IsInRole(DbSeeder.RoleVeteriner)
            && pet.OwnerId != userId)
            return BadRequest(new { message = "Bu hayvana erişim yetkiniz yok." });

        return Ok(_petService.GetWithVaccineHistory(id));
    }

    /// <summary>Yaklaşan aşıları olan tüm evcil hayvanları getirir (LINQ raporu).</summary>
    [HttpGet("upcoming-vaccines")]
    [Authorize(Roles = DbSeeder.RoleAdmin + "," + DbSeeder.RoleVeteriner)]
    [ProducesResponseType(typeof(IEnumerable<PetDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<PetDto>> GetUpcomingVaccines([FromQuery] int daysAhead = 30)
    {
        return Ok(_petService.GetPetsWithUpcomingVaccines(daysAhead));
    }
}