using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services.Dtos;

namespace VeterinerKlinigi.Services;

public class AppointmentService : IAppointmentService
{
    private readonly ApplicationDbContext _db;

    public AppointmentService(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Bir veteriner için seçilen hizmet ve tarihteki uygun başlangıç saatlerini hesaplar.
    /// Kullanılacak saatler: müsaitlik aralığı içinde, hizmet süresine sığan,
    /// çakışmayan ve henüz geçmemiş saatlerdir.
    /// </summary>
    public List<AvailableSlot> GetAvailableSlots(int veterinarianId, int serviceId, DateTime date)
    {
        var slots = new List<AvailableSlot>();
        var day = date.Date;

        var vet = _db.Veterinarians
            .Include(v => v.Availabilities)
            .FirstOrDefault(v => v.Id == veterinarianId && v.IsActive);
        if (vet == null) return slots;

        var providesService = _db.VeterinarianServices
            .Any(vs => vs.VeterinarianId == veterinarianId && vs.ServiceId == serviceId);
        if (!providesService) return slots;

        var service = _db.Services.FirstOrDefault(s => s.Id == serviceId);
        if (service == null) return slots;

        var availability = vet.Availabilities.FirstOrDefault(a => a.DayOfWeek == day.DayOfWeek);
        if (availability == null) return slots;

        // O gün bu veterinerin onaylanmış/uygulanmış randevuları.
        var existing = _db.Appointments
            .Where(a => a.VeterinarianId == veterinarianId
                     && a.AppointmentDate.Date == day
                     && a.Status != AppointmentStatus.Reddedildi
                     && a.Status != AppointmentStatus.Iptal)
            .Select(a => new { a.AppointmentDate, a.DurationMinutes })
            .ToList();

        var duration = TimeSpan.FromMinutes(service.DurationMinutes);
        var slotStep = TimeSpan.FromMinutes(30);
        var now = DateTime.Now;

        var cursor = day + availability.StartTime;
        var windowEnd = day + availability.EndTime;

        while (cursor + duration <= windowEnd)
        {
            var slotEnd = cursor + duration;
            var overlaps = existing.Any(e =>
                cursor < e.AppointmentDate.AddMinutes(e.DurationMinutes)
                && slotEnd > e.AppointmentDate);
            var inPast = cursor <= now;

            if (!overlaps && !inPast)
            {
                slots.Add(new AvailableSlot { Start = cursor, End = slotEnd });
            }

            cursor += slotStep;
        }

        return slots;
    }

    public AppointmentResult CheckSlot(int veterinarianId, int serviceId, DateTime appointmentDate)
    {
        var vet = _db.Veterinarians.FirstOrDefault(v => v.Id == veterinarianId && v.IsActive);
        if (vet == null)
            return new AppointmentResult { Success = false, Message = "Veteriner bulunamadı veya aktif değil." };

        if (!_db.VeterinarianServices.Any(vs => vs.VeterinarianId == veterinarianId && vs.ServiceId == serviceId))
            return new AppointmentResult { Success = false, Message = "Seçilen veteriner bu hizmeti vermiyor." };

        var service = _db.Services.FirstOrDefault(s => s.Id == serviceId);
        if (service == null)
            return new AppointmentResult { Success = false, Message = "Hizmet bulunamadı." };

        var day = appointmentDate.Date;
        var availability = _db.Availabilities.FirstOrDefault(
            a => a.VeterinarianId == veterinarianId && a.DayOfWeek == day.DayOfWeek);
        if (availability == null)
            return new AppointmentResult { Success = false, Message = "Veteriner seçilen günde müsait değil." };

        var duration = TimeSpan.FromMinutes(service.DurationMinutes);
        var start = appointmentDate.TimeOfDay;
        var end = start + duration;

        if (start < availability.StartTime || end > availability.EndTime)
            return new AppointmentResult { Success = false, Message = $"Hizmet süresi ({service.DurationMinutes} dk) seçilen çalışma aralığına sığmıyor." };

        if (appointmentDate <= DateTime.Now)
            return new AppointmentResult { Success = false, Message = "Geçmiş bir tarihe randevu alınamaz." };

        var candidates = _db.Appointments
            .Where(a => a.VeterinarianId == veterinarianId
                     && a.AppointmentDate.Date == day
                     && a.Status != AppointmentStatus.Reddedildi
                     && a.Status != AppointmentStatus.Iptal)
            .Select(a => new { a.AppointmentDate, a.DurationMinutes })
            .ToList();

        var overlaps = candidates.Any(a =>
            appointmentDate < a.AppointmentDate.AddMinutes(a.DurationMinutes)
            && end > a.AppointmentDate.TimeOfDay);

        if (overlaps)
            return new AppointmentResult { Success = false, Message = "Seçilen saat bu veterinerde başka bir randevuyla çakışıyor." };

        return new AppointmentResult { Success = true, Message = "Uygun." };
    }

    public AppointmentResult CreateAppointment(int petId, CreateAppointmentRequest request)
    {
        var check = CheckSlot(request.VeterinarianId, request.ServiceId, request.AppointmentDate);
        if (!check.Success)
            return check;

        var pet = _db.Pets.FirstOrDefault(p => p.Id == petId);
        if (pet == null)
            return new AppointmentResult { Success = false, Message = "Evcil hayvan bulunamadı." };

        var service = _db.Services.First(s => s.Id == request.ServiceId);

        var appointment = new Appointment
        {
            PetId = petId,
            ServiceId = request.ServiceId,
            VeterinarianId = request.VeterinarianId,
            AppointmentDate = request.AppointmentDate,
            DurationMinutes = service.DurationMinutes,
            Price = service.Price,
            Status = AppointmentStatus.Bekliyor,
            Notes = request.Notes,
            CreatedAt = DateTime.UtcNow
        };

        _db.Appointments.Add(appointment);
        _db.SaveChanges();

        return new AppointmentResult
        {
            Success = true,
            Message = "Randevunuz başarıyla oluşturuldu ve onay bekliyor.",
            AppointmentId = appointment.Id
        };
    }

    public AppointmentResult Approve(int appointmentId)
    {
        var appointment = _db.Appointments.FirstOrDefault(a => a.Id == appointmentId);
        if (appointment == null)
            return new AppointmentResult { Success = false, Message = "Randevu bulunamadı." };
        if (appointment.Status != AppointmentStatus.Bekliyor)
            return new AppointmentResult { Success = false, Message = "Yalnızca bekleyen randevular onaylanabilir." };

        appointment.Status = AppointmentStatus.Onaylandi;
        _db.SaveChanges();
        return new AppointmentResult { Success = true, Message = "Randevu onaylandı." };
    }

    public AppointmentResult Reject(int appointmentId, string reason)
    {
        var appointment = _db.Appointments.FirstOrDefault(a => a.Id == appointmentId);
        if (appointment == null)
            return new AppointmentResult { Success = false, Message = "Randevu bulunamadı." };
        if (appointment.Status != AppointmentStatus.Bekliyor)
            return new AppointmentResult { Success = false, Message = "Yalnızca bekleyen randevular reddedilebilir." };

        appointment.Status = AppointmentStatus.Reddedildi;
        appointment.RejectionReason = reason;
        _db.SaveChanges();
        return new AppointmentResult { Success = true, Message = "Randevu reddedildi." };
    }

    public AppointmentResult Complete(int appointmentId)
    {
        var appointment = _db.Appointments.FirstOrDefault(a => a.Id == appointmentId);
        if (appointment == null)
            return new AppointmentResult { Success = false, Message = "Randevu bulunamadı." };
        if (appointment.Status != AppointmentStatus.Onaylandi)
            return new AppointmentResult { Success = false, Message = "Yalnızca onaylanmış randevular tamamlanabilir." };

        appointment.Status = AppointmentStatus.Tamamlandi;
        _db.SaveChanges();
        return new AppointmentResult { Success = true, Message = "Randevu tamamlandı." };
    }

    public List<AppointmentDto> GetAppointmentsForVeterinarian(int veterinarianId)
    {
        return _db.Appointments
            .Where(a => a.VeterinarianId == veterinarianId)
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
            })
            .ToList();
    }
}