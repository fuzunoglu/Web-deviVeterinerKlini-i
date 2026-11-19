using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;

namespace VeterinerKlinigi.Services;

public class VeterinarianService : IVeterinarianService
{
    private readonly ApplicationDbContext _db;
    private readonly IAppointmentService _appointmentService;

    public VeterinarianService(ApplicationDbContext db, IAppointmentService appointmentService)
    {
        _db = db;
        _appointmentService = appointmentService;
    }

    public List<VeterinarianDto> GetAll()
    {
        return _db.Veterinarians
            .Where(v => v.IsActive)
            .Include(v => v.Clinic)
            .Include(v => v.VeterinarianServices).ThenInclude(vs => vs.Service)
            .Select(v => new VeterinarianDto
            {
                Id = v.Id,
                FullName = v.FullName,
                Specialties = v.Specialties,
                ClinicId = v.ClinicId,
                ClinicName = v.Clinic!.Name,
                Services = v.VeterinarianServices.Select(vs => vs.Service!.Name).ToList()
            })
            .ToList();
    }

    /// <summary>Belirli bir tarih ve hizmet için müsait olan veterinerleri döndürür (LINQ).</summary>
    public List<VeterinarianDto> GetByServiceAndDate(int serviceId, DateTime date)
    {
        var dayOfWeek = date.DayOfWeek;

        // Hizmeti veren ve o gün müsait olan aktif veterinerler (LINQ sorgusu).
        var candidates = _db.Veterinarians
            .Where(v => v.IsActive
                     && v.VeterinarianServices.Any(vs => vs.ServiceId == serviceId)
                     && v.Availabilities.Any(a => a.DayOfWeek == dayOfWeek))
            .Include(v => v.Clinic)
            .Include(v => v.VeterinarianServices).ThenInclude(vs => vs.Service)
            .ToList();

        var result = new List<VeterinarianDto>();
        foreach (var v in candidates)
        {
            var slots = _appointmentService.GetAvailableSlots(v.Id, serviceId, date);
            if (slots.Count > 0)
            {
                result.Add(new VeterinarianDto
                {
                    Id = v.Id,
                    FullName = v.FullName,
                    Specialties = v.Specialties,
                    ClinicId = v.ClinicId,
                    ClinicName = v.Clinic!.Name,
                    Services = v.VeterinarianServices.Select(vs => vs.Service!.Name).ToList()
                });
            }
        }

        return result;
    }

    public List<DateTime> GetAvailableTimes(int veterinarianId, int serviceId, DateTime date)
    {
        return _appointmentService.GetAvailableSlots(veterinarianId, serviceId, date)
            .Select(s => s.Start)
            .ToList();
    }

    public VeterinarianDto? GetById(int id)
    {
        var v = _db.Veterinarians
            .Include(v => v.Clinic)
            .Include(v => v.VeterinarianServices).ThenInclude(vs => vs.Service)
            .FirstOrDefault(v => v.Id == id);
        if (v == null) return null;
        return new VeterinarianDto
        {
            Id = v.Id,
            FullName = v.FullName,
            Specialties = v.Specialties,
            ClinicId = v.ClinicId,
            ClinicName = v.Clinic!.Name,
            Services = v.VeterinarianServices.Select(vs => vs.Service!.Name).ToList()
        };
    }
}