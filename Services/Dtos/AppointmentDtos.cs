namespace VeterinerKlinigi.Services.Dtos;

/// <summary>Randevu oluşturma isteği.</summary>
public class CreateAppointmentRequest
{
    public int PetId { get; set; }
    public int ServiceId { get; set; }
    public int VeterinarianId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Randevu oluşturma sonucu.</summary>
public class AppointmentResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int? AppointmentId { get; set; }
}

/// <summary>Uygun saat listesi için bir öğe.</summary>
public class AvailableSlot
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
}

/// <summary>Randevu listesi için DTO (entity döndürmemek için).</summary>
public class AppointmentDto
{
    public int Id { get; set; }
    public string PetName { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string VeterinarianName { get; set; } = string.Empty;
    public DateTime AppointmentDate { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
}