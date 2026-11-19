using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>Veteriner kliniği.</summary>
public class Clinic
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Klinik adı zorunludur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Klinik adı 2-100 karakter olmalıdır.")]
    [Display(Name = "Klinik Adı")]
    public string Name { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Address { get; set; }

    [Phone(ErrorMessage = "Geçerli bir telefon giriniz.")]
    [StringLength(20)]
    public string? Phone { get; set; }

    [EmailAddress(ErrorMessage = "Geçerli bir e-posta giriniz.")]
    [StringLength(150)]
    public string? Email { get; set; }

    [StringLength(1000)]
    public string? Description { get; set; }

    [Display(Name = "Açılış Saati")]
    [DataType(DataType.Time)]
    public TimeSpan OpeningTime { get; set; } = new(9, 0, 0);

    [Display(Name = "Kapanış Saati")]
    [DataType(DataType.Time)]
    public TimeSpan ClosingTime { get; set; } = new(18, 0, 0);

    public ICollection<Service> Services { get; set; } = new List<Service>();
    public ICollection<Veterinarian> Veterinarians { get; set; } = new List<Veterinarian>();
}