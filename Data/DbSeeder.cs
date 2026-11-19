using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Data;

/// <summary>
/// Veritabanını migrasyona hazırlar; roller, admin ve örnek verileri oluşturur.
/// </summary>
public static class DbSeeder
{
    public const string RoleAdmin = "Admin";
    public const string RoleVeteriner = "Veteriner";
    public const string RoleUye = "Uye";

    public static async Task SeedAsync(IServiceProvider services)
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        // Migration'ları uygula.
        await db.Database.MigrateAsync();

        // Roller.
        foreach (var role in new[] { RoleAdmin, RoleVeteriner, RoleUye })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new IdentityRole(role));
        }

        // Admin kullanıcısı (ödev: ogrencinumarasi@sakarya.edu.tr / sau).
        var adminEmail = config["Admin:Email"] ?? "ogrencinumarasi@sakarya.edu.tr";
        var adminPassword = config["Admin:Password"] ?? "sau";
        var admin = await userManager.FindByEmailAsync(adminEmail);
        if (admin == null)
        {
            admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FullName = "Sistem Yöneticisi",
                EmailConfirmed = true
            };
            var result = await userManager.CreateAsync(admin, adminPassword);
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, RoleAdmin);
        }

        await SeedSampleDataAsync(db, userManager);
    }

    private static async Task SeedSampleDataAsync(ApplicationDbContext db, UserManager<ApplicationUser> userManager)
    {
        if (db.Clinics.Any())
            return; // Zaten veri var.

        // --- Klinik ---
        var clinic = new Clinic
        {
            Name = "Patili Dostlar Veteriner Kliniği",
            Address = "Adapazarı / Sakarya",
            Phone = "0264 000 00 00",
            Email = "info@patilidostlar.example",
            Description = "Küçük ve egzotik hayvanlar için modern bakım merkezi.",
            OpeningTime = new TimeSpan(9, 0, 0),
            ClosingTime = new TimeSpan(18, 0, 0)
        };
        db.Clinics.Add(clinic);
        await db.SaveChangesAsync();

        // --- Hizmetler ---
        var muayene = new Service { ClinicId = clinic.Id, Name = "Genel Muayene", DurationMinutes = 30, Price = 300, Description = "Rutin sağlık kontrolü." };
        var asi = new Service { ClinicId = clinic.Id, Name = "Aşı Uygulama", DurationMinutes = 15, Price = 250, Description = "Rutin aşıların uygulanması." };
        var kisl = new Service { ClinicId = clinic.Id, Name = "Kısırlaştırma", DurationMinutes = 120, Price = 2500, Description = "Cerrahi kısırlaştırma operasyonu." };
        var dis = new Service { ClinicId = clinic.Id, Name = "Diş Temizliği", DurationMinutes = 45, Price = 800, Description = "Diş taşı temizliği." };
        var bakim = new Service { ClinicId = clinic.Id, Name = "Tıraş ve Bakım", DurationMinutes = 60, Price = 500, Description = "Tüy bakımı ve tıraş." };
        db.Services.AddRange(muayene, asi, kisl, dis, bakim);
        await db.SaveChangesAsync();

        // --- Veterinerler ---
        var vet1 = new Veterinarian { ClinicId = clinic.Id, FullName = "Dr. Elif Yılmaz", Email = "elif@patilidostlar.example", Specialties = "Küçük Hayvan, Dermatoloji", IsActive = true };
        var vet2 = new Veterinarian { ClinicId = clinic.Id, FullName = "Dr. Mert Demir", Email = "mert@patilidostlar.example", Specialties = "Cerrahi, Egzotik Hayvan", IsActive = true };
        db.Veterinarians.AddRange(vet1, vet2);
        await db.SaveChangesAsync();

        // --- Hekim-hizmet eşleşmeleri ---
        db.VeterinarianServices.AddRange(
            new VeterinarianService { VeterinarianId = vet1.Id, ServiceId = muayene.Id },
            new VeterinarianService { VeterinarianId = vet1.Id, ServiceId = asi.Id },
            new VeterinarianService { VeterinarianId = vet1.Id, ServiceId = dis.Id },
            new VeterinarianService { VeterinarianId = vet1.Id, ServiceId = bakim.Id },
            new VeterinarianService { VeterinarianId = vet2.Id, ServiceId = muayene.Id },
            new VeterinarianService { VeterinarianId = vet2.Id, ServiceId = kisl.Id },
            new VeterinarianService { VeterinarianId = vet2.Id, ServiceId = asi.Id }
        );

        // --- Müsaitlik saatleri (Pzt-Cuma 09:00-17:00) ---
        foreach (var day in new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday })
        {
            db.Availabilities.Add(new Availability { VeterinarianId = vet1.Id, DayOfWeek = day, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0) });
            db.Availabilities.Add(new Availability { VeterinarianId = vet2.Id, DayOfWeek = day, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(17, 0, 0) });
        }
        await db.SaveChangesAsync();

        // --- Örnek üye (hayvan sahibi) ---
        var owner = await userManager.FindByEmailAsync("pamuk.sahibi@example.com");
        if (owner == null)
        {
            owner = new ApplicationUser
            {
                UserName = "pamuk.sahibi@example.com",
                Email = "pamuk.sahibi@example.com",
                FullName = "Ayşe Kaya",
                EmailConfirmed = true
            };
            await userManager.CreateAsync(owner, "Uye123!");
            await userManager.AddToRoleAsync(owner, RoleUye);
        }

        // --- Örnek evcil hayvanlar ---
        var pamuk = new Pet { OwnerId = owner.Id, Name = "Pamuk", Species = "Kedi", Breed = "Tekir", BirthDate = new DateTime(2021, 5, 10), Weight = 4.2m };
        var boncuk = new Pet { OwnerId = owner.Id, Name = "Boncuk", Species = "Köpek", Breed = "Golden", BirthDate = new DateTime(2020, 3, 2), Weight = 28.5m };
        db.Pets.AddRange(pamuk, boncuk);
        await db.SaveChangesAsync();

        // --- Aşı kayıtları ---
        db.VaccineRecords.AddRange(
            new VaccineRecord { PetId = pamuk.Id, VaccineName = "Kuduz", AdministeredDate = new DateTime(2026, 1, 15), NextDueDate = new DateTime(2027, 1, 15) },
            new VaccineRecord { PetId = pamuk.Id, VaccineName = "FVRCP", AdministeredDate = new DateTime(2026, 1, 15), NextDueDate = new DateTime(2026, 12, 20) },
            new VaccineRecord { PetId = boncuk.Id, VaccineName = "Kuduz", AdministeredDate = new DateTime(2026, 2, 1), NextDueDate = new DateTime(2027, 2, 1) }
        );
        await db.SaveChangesAsync();

        // --- Örnek veteriner kullanıcı hesabı ---
        var vetUser = await userManager.FindByEmailAsync("dr.elif@patilidostlar.example");
        if (vetUser == null)
        {
            vetUser = new ApplicationUser
            {
                UserName = "dr.elif@patilidostlar.example",
                Email = "dr.elif@patilidostlar.example",
                FullName = "Dr. Elif Yılmaz",
                EmailConfirmed = true,
                VeterinarianId = vet1.Id
            };
            await userManager.CreateAsync(vetUser, "Vet123!");
            await userManager.AddToRoleAsync(vetUser, RoleVeteriner);
        }
    }
}