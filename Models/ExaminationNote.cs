using System.ComponentModel.DataAnnotations;

namespace VeterinerKlinigi.Models;

/// <summary>Tamamlanan randevu için veterinerin girdiği muayene notu.</summary>
public class ExaminationNote
{
    public int Id { get; set; }

    public int AppointmentId { get; set; }
    public Appointment? Appointment { get; set; }

    [Required(ErrorMessage = "Muayene notu zorunludur.")]
    [StringLength(2000, MinimumLength = 3)]
    [Display(Name = "Muayene Notu")]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}