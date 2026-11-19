using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>Evcil hayvanın aşı kaydı.</summary>
public class VaccineRecord
{
    public int Id { get; set; }

    public int PetId { get; set; }
    public Pet? Pet { get; set; }

    [Required(ErrorMessage = "Aşı adı zorunludur.")]
    [StringLength(100)]
    [Display(Name = "Aşı Adı")]
    public string VaccineName { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Yapılma Tarihi")]
    public DateTime AdministeredDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    [Display(Name = "Bir Sonraki Doz Tarihi")]
    public DateTime? NextDueDate { get; set; }

    [StringLength(500)]
    [Display(Name = "Not")]
    public string? Notes { get; set; }
}