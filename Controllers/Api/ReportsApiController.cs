using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Services;

namespace VeterinerKlinigi.Controllers.Api;

[ApiController]
[Route("api/reports")]
[Authorize(Roles = DbSeeder.RoleAdmin + "," + DbSeeder.RoleVeteriner)]
public class ReportsApiController : ControllerBase
{
    private readonly IReportService _reportService;

    public ReportsApiController(IReportService reportService)
    {
        _reportService = reportService;
    }

    /// <summary>Her hizmet türü için randevu sayısı ve ciroları getirir (LINQ).</summary>
    [HttpGet("by-service")]
    [ProducesResponseType(typeof(IEnumerable<ServiceReportRow>), StatusCodes.Status200OK)]
    public ActionResult<IEnumerable<ServiceReportRow>> GetServiceReport(
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null)
    {
        if (from.HasValue && to.HasValue && from.Value > to.Value)
            return BadRequest(new { message = "'from' tarihi 'to' tarihinden sonra olamaz." });

        return Ok(_reportService.GetServiceReport(from, to));
    }
}