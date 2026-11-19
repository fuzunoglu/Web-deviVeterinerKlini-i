using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>Hayvan sahibine ait evcil hayvan.</summary>
public class Pet
{
    public int Id { get; set; }

    [Display(Name = "Hayvan Sahibi")]
    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser? Owner { get; set; }

    [Required(ErrorMessage = "Evcil hayvan adı zorunludur.")]
    [StringLength(100, MinimumLength = 1)]
    [Display(Name = "Ad")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Tür zorunludur.")]
    [StringLength(50)]
    [Display(Name = "Tür")]
    public string Species { get; set; } = string.Empty; // Köpek, Kedi, Kuş vb.

    [StringLength(50)]
    [Display(Name = "Irk")]
    public string? Breed { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Doğum Tarihi")]
    public DateTime? BirthDate { get; set; }

    [Range(0, 1000, ErrorMessage = "Kilo 0-1000 kg arasında olmalıdır.")]
    [Display(Name = "Kilo (kg)")]
    public decimal? Weight { get; set; }

    [StringLength(250)]
    [Display(Name = "Fotoğraf")]
    public string? PhotoPath { get; set; }

    public ICollection<VaccineRecord> VaccineRecords { get; set; } = new List<VaccineRecord>();
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}