using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services.Dtos;

namespace VeterinerKlinigi.Services;

/// <summary>
/// Randevu iş kuralları. MVC, REST API ve MCP aynı bu servisi kullanır.
/// </summary>
public interface IAppointmentService
{
    /// <summary>Seçilen veteriner için verilen tarihte uygun saatleri döndürür.</summary>
    List<AvailableSlot> GetAvailableSlots(int veterinarianId, int serviceId, DateTime date);

    /// <summary>Tek bir randevu zamanının uygun olup olmadığını kontrol eder.</summary>
    AppointmentResult CheckSlot(int veterinarianId, int serviceId, DateTime appointmentDate);

    /// <summary>Uygunluk kontrolünden sonra randevu oluşturur (durum: Bekliyor).</summary>
    AppointmentResult CreateAppointment(int petId, CreateAppointmentRequest request);

    /// <summary>Bekleyen randevuyu onaylar.</summary>
    AppointmentResult Approve(int appointmentId);

    /// <summary>Bekleyen randevuyu gerekçesiyle reddeder.</summary>
    AppointmentResult Reject(int appointmentId, string reason);

    /// <summary>Randevuyu tamamlanmış işaretler.</summary>
    AppointmentResult Complete(int appointmentId);

    /// <summary>Mevcut randevuyu durumda günceller (onay/ret).</summary>
    List<AppointmentDto> GetAppointmentsForVeterinarian(int veterinarianId);
}