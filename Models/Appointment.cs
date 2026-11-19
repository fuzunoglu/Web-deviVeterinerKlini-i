using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>Randevu kaydı.</summary>
public class Appointment
{
    public int Id { get; set; }

    [Display(Name = "Evcil Hayvan")]
    public int PetId { get; set; }
    public Pet? Pet { get; set; }

    [Display(Name = "Hizmet")]
    public int ServiceId { get; set; }
    public Service? Service { get; set; }

    [Display(Name = "Veteriner")]
    public int VeterinarianId { get; set; }
    public Veterinarian? Veterinarian { get; set; }

    [DataType(DataType.DateTime)]
    [Display(Name = "Tarih ve Saat")]
    public DateTime AppointmentDate { get; set; }

    [Display(Name = "Süre (dakika)")]
    public int DurationMinutes { get; set; }

    [Display(Name = "Ücret (₺)")]
    public decimal Price { get; set; }

    [Display(Name = "Durum")]
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Bekliyor;

    [StringLength(500)]
    [Display(Name = "Not")]
    public string? Notes { get; set; }

    [StringLength(500)]
    [Display(Name = "Red/Neden")]
    public string? RejectionReason { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ExaminationNote? ExaminationNote { get; set; }
}