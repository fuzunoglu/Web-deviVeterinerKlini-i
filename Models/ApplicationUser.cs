using Microsoft.AspNetCore.Identity;

namespace VeterinerKlinigi.Models;

/// <summary>
/// Uygulama kullanıcısı. ASP.NET Core Identity'i genişletir.
/// Admin, Veteriner ve Üye rolleri atanabilir.
/// </summary>
public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Bir veteriner hekim bu kullanıcıya bağlıysa doldurulur.
    public int? VeterinarianId { get; set; }
    public Veterinarian? Veterinarian { get; set; }

    // Bu kullanıcıya ait evcil hayvanlar (sadece Üye rolü için).
    public ICollection<Pet> Pets { get; set; } = new List<Pet>();
}