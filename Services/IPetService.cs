namespace VeterinerKlinigi.Services;

public class VaccineDto
{
    public int Id { get; set; }
    public string VaccineName { get; set; } = string.Empty;
    public DateTime AdministeredDate { get; set; }
    public DateTime? NextDueDate { get; set; }
    public bool IsUpcoming { get; set; }
    public int DaysUntilDue { get; set; }
}

public class PetDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Species { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public DateTime? BirthDate { get; set; }
    public decimal? Weight { get; set; }
    public string? PhotoPath { get; set; }
    public string OwnerId { get; set; } = string.Empty;
    public List<VaccineDto> Vaccines { get; set; } = new();
    public List<VaccineDto> UpcomingVaccines { get; set; } = new();
}

public interface IPetService
{
    PetDto? GetById(int id);
    List<PetDto> GetByOwner(string ownerId);
    PetDto? GetWithVaccineHistory(int id);
    List<PetDto> GetPetsWithUpcomingVaccines(int daysAhead = 30);
}