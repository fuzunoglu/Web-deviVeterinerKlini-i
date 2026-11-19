using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>Klinikte görev yapan veteriner hekim.</summary>
public class Veterinarian
{
    public int Id { get; set; }

    [Display(Name = "Klinik")]
    public int ClinicId { get; set; }
    public Clinic? Clinic { get; set; }

    [Required(ErrorMessage = "Ad soyad zorunludur.")]
    [StringLength(100, MinimumLength = 2)]
    [Display(Name = "Ad Soyad")]
    public string FullName { get; set; } = string.Empty;

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [StringLength(150)]
    public string? Email { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon giriniz.")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [StringLength(250)]
    [Display(Name = "Uzmanlık Alanları")]
    [DisplayFormat(NullDisplayText = "Belirtilmedi")]
    public string? Specialties { get; set; }

    [StringLength(1000)]
    public string? Biography { get; set; }

    public bool IsActive { get; set; } = true;

    // Bu hekimin verebildiği hizmetler.
    public ICollection<VeterinarianService> VeterinarianServices { get; set; } = new List<VeterinarianService>();
    // Bu hekimin müsaitlik saatleri.
    public ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();

    public ApplicationUser? User { get; set; }
}