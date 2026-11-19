using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Data;

namespace VeterinerKlinigi.Services;

public class PetService : IPetService
{
    private readonly ApplicationDbContext _db;

    public PetService(ApplicationDbContext db)
    {
        _db = db;
    }

    public PetDto? GetById(int id)
    {
        var p = _db.Pets.Include(p => p.Owner).FirstOrDefault(p => p.Id == id);
        if (p == null) return null;
        return Map(p);
    }

    public List<PetDto> GetByOwner(string ownerId)
    {
        return _db.Pets
            .Where(p => p.OwnerId == ownerId)
            .OrderBy(p => p.Name)
            .Select(p => Map(p))
            .ToList();
    }

    /// <summary>Evcil hayvanın aşı geçmişini ve yaklaşan aşılarını döndürür (LINQ).</summary>
    public PetDto? GetWithVaccineHistory(int id)
    {
        var p = _db.Pets
            .Include(p => p.VaccineRecords)
            .FirstOrDefault(p => p.Id == id);
        if (p == null) return null;

        var today = DateTime.Today;
        var dto = Map(p);
        dto.Vaccines = p.VaccineRecords
            .OrderBy(v => v.AdministeredDate)
            .Select(v => new VaccineDto
            {
                Id = v.Id,
                VaccineName = v.VaccineName,
                AdministeredDate = v.AdministeredDate,
                NextDueDate = v.NextDueDate,
                IsUpcoming = v.NextDueDate.HasValue && v.NextDueDate.Value.Date <= today.AddDays(30),
                DaysUntilDue = v.NextDueDate.HasValue ? (v.NextDueDate.Value.Date - today).Days : 0
            })
            .ToList();
        dto.UpcomingVaccines = dto.Vaccines
            .Where(v => v.NextDueDate.HasValue && v.NextDueDate.Value.Date <= today.AddDays(30))
            .OrderBy(v => v.NextDueDate)
            .ToList();

        return dto;
    }

    public List<PetDto> GetPetsWithUpcomingVaccines(int daysAhead = 30)
    {
        var today = DateTime.Today;
        var limit = today.AddDays(daysAhead);

        var pets = _db.Pets.Include(p => p.VaccineRecords).ToList();
        var result = new List<PetDto>();
        foreach (var p in pets)
        {
            var dto = Map(p);
            dto.UpcomingVaccines = p.VaccineRecords
                .Where(v => v.NextDueDate.HasValue && v.NextDueDate.Value.Date >= today && v.NextDueDate.Value.Date <= limit)
                .OrderBy(v => v.NextDueDate)
                .Select(v => new VaccineDto
                {
                    Id = v.Id,
                    VaccineName = v.VaccineName,
                    AdministeredDate = v.AdministeredDate,
                    NextDueDate = v.NextDueDate,
                    IsUpcoming = true,
                    DaysUntilDue = (v.NextDueDate!.Value.Date - today).Days
                })
                .ToList();
            if (dto.UpcomingVaccines.Count > 0)
                result.Add(dto);
        }
        return result;
    }

    private static PetDto Map(Models.Pet p) => new()
    {
        Id = p.Id,
        Name = p.Name,
        Species = p.Species,
        Breed = p.Breed,
        BirthDate = p.BirthDate,
        Weight = p.Weight,
        PhotoPath = p.PhotoPath,
        OwnerId = p.OwnerId
    };
}