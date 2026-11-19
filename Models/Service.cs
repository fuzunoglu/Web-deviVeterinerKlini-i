using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>Klinikte sunulan hizmet (muayene, aşı, kısırlaştırma vb.).</summary>
public class Service
{
    public int Id { get; set; }

    [Display(Name = "Klinik")]
    public int ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    [Required(ErrorMessage = "Hizmet adı zorunludur.")]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Hizmet Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Display(Name = "Açıklama")]
    public string? Description { get; set; }

    [Range(5, 480, ErrorMessage = "Süre 5-480 dakika arasında olmalıdır.")]
    [Display(Name = "Süre (dakika)")]
    public int DurationMinutes { get; set; }

    [Range(0, 100000, ErrorMessage = "Ücret 0-100000 arasında olmalıdır.")]
    [Display(Name = "Ücret (₺)")]
    public decimal Price { get; set; }

    public ICollection<VeterinarianService> VeterinarianServices { get; set; } = new List<VeterinarianService>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}