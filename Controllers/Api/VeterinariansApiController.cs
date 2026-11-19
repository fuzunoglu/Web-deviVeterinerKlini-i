using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Services;

namespace VeterinerKlinigi.Controllers.Api;

[ApiController]
[Route("api/veterinarians")]
[Authorize]
public class VeterinariansApiController : ControllerBase
{
    private readonly IVeterinarianService _service;

    public VeterinariansApiController(IVeterinarianService service)
    {
        _service = service;
    }

    /// <summary>Tüm aktif veteriner hekimleri listeler.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<VeterinarianDto>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<VeterinarianDto>> GetAll()
    {
        return Ok(_service.GetAll());
    }

    /// <summary>Belirli bir tarihte, seçilen hizmet için uygun veterinerleri getirir (LINQ).</summary>
    [HttpGet("available")]
    [ProducesResponseType(typeof(IEnumerable<VeterinarianDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<IEnumerable<VeterinarianDto>> GetAvailable(
        [FromQuery] int serviceId,
        [FromQuery] DateTime date)
    {
        if (serviceId <= 0)
            return BadRequest(new { message = "serviceId pozitif olmalıdır." });

        var result = _service.GetByServiceAndDate(serviceId, date);
        return Ok(result);
    }

    /// <summary>Belirli bir veterinerin seçili hizmet ve tarihteki müsait saatlerini getirir.</summary>
    [HttpGet("{id:int}/slots")]
    [ProducesResponseType(typeof(IEnumerable<DateTime>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<IEnumerable<DateTime>> GetSlots(int id, [FromQuery] int serviceId, [FromQuery] DateTime date)
    {
        if (_service.GetById(id) == null)
            return NotFound(new { message = "Veteriner bulunamadı." });

        return Ok(_service.GetAvailableTimes(id, serviceId, date));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(VeterinarianDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public ActionResult<VeterinarianDto> GetById(int id)
    {
        var vet = _service.GetById(id);
        if (vet == null)
            return NotFound(new { message = "Veteriner bulunamadı." });
        return Ok(vet);
    }
}