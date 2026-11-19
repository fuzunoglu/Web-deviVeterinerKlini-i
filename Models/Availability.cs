using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>Veteriner hekimin haftalık müsaitlik aralığı.</summary>
public class Availability
{
    public int Id { get; set; }

    public int VeterinarianId { get; set; }
    public Veterinarian? Veterinarian { get; set; }

    [Display(Name = "Gün")]
    public DayOfWeek DayOfWeek { get; set; }

    [Display(Name = "Başlangıç")]
    public TimeSpan StartTime { get; set; }

    [Display(Name = "Bitiş")]
    public TimeSpan EndTime { get; set; }
}