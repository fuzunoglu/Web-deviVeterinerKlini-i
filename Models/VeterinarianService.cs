namespace VeterinerKlinigi.Models;

/// <summary>Bir veterinerin verebildiği hizmetleri eşler.</summary>
public class VeterinarianService
{
    public int VeterinarianId { get; set; }
    public Veterinarian? Veterinarian { get; set; }

    public int ServiceId { get; set; }
    public Service? Service { get; set; }
}