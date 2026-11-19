using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services.Dtos;

namespace VeterinerKlinigi.Services;

public class VeterinarianDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Specialties { get; set; }
    public int ClinicId { get; set; }
    public string ClinicName { get; set; } = string.Empty;
    public IReadOnlyList<string> Services { get; set; } = Array.Empty<string>();
}

public interface IVeterinarianService
{
    List<VeterinarianDto> GetAll();
    List<VeterinarianDto> GetByServiceAndDate(int serviceId, DateTime date);
    List<DateTime> GetAvailableTimes(int veterinarianId, int serviceId, DateTime date);
    VeterinarianDto? GetById(int id);
}