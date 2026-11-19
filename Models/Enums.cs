namespace VeterinerKlinigi.Models;

/// <summary>
/// Randevu durumları.
/// </summary>
public enum AppointmentStatus
{
    Bekliyor = 0,
    Onaylandi = 1,
    Reddedildi = 2,
    Tamamlandi = 3,
    Iptal = 4
}