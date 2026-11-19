using System.ComponentModel;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ModelContextProtocol.Server;
using VeterinerKlinigi.Data;
using VeterinerKlinigi.Models;
using VeterinerKlinigi.Services;
using VeterinerKlinigi.Services.Dtos;

namespace VeterinerKlinigi.Mcp;

/// <summary>
/// Yapay zekâ asistanlarının (Claude Desktop, VS Code, MCP Inspector vb.)
/// erişebildiği MCP araçları. Tüm araçlar MVC ve REST API ile aynı servis
/// katmanını kullanır (iş kuralları tek yerde). Veriler yalnızca oturum açan
/// kullanıcının kendi kayıtlarıyla sınırlıdır.
/// </summary>
[McpServerToolType]
public class VeterinerMcpTools
{
    private readonly IAppointmentService _appointmentService;
    private readonly IVeterinarianService _veterinarianService;
    private readonly IPetService _petService;
    private readonly ApplicationDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly UserManager<ApplicationUser> _userManager;

    public VeterinerMcpTools(
        IAppointmentService appointmentService,
        IVeterinarianService veterinarianService,
        IPetService petService,
        ApplicationDbContext db,
        IHttpContextAccessor http,
        UserManager<ApplicationUser> userManager)
    {
        _appointmentService = appointmentService;
        _veterinarianService = veterinarianService;
        _petService = petService;
        _db = db;
        _http = http;
        _userManager = userManager;
    }

    private string? CurrentUserId =>
        _http.HttpContext?.User?.Identity?.IsAuthenticated == true
            ? _userManager.GetUserId(_http.HttpContext.User)
            : null;

    private static bool TryParseDate(string date, out DateTime result)
        => DateTime.TryParse(date, out result);

    [McpServerTool(Name = "uygun_saatler")]
    [Description("Belirli bir hizmet ve tarih için boşta olan veteriner hekimleri ve müsait saatleri listeler.")]
    public async Task<string> UygunSaatler(
        [Description("Hizmet ID'si (örn. 1=Genel Muayene).")] int hizmetId,
        [Description("Tarih (yyyy-MM-dd).")] string tarih)
    {
        if (!TryParseDate(tarih, out var date))
            return $"Geçersiz tarih: '{tarih}'. Lütfen yyyy-MM-dd biçiminde girin.";

        var vets = _veterinarianService.GetByServiceAndDate(hizmetId, date.Date);
        if (vets.Count == 0)
            return $"{date:dd.MM.yyyy} tarihi için bu hizmeti verebilecek uygun veteriner bulunamadı.";

        var lines = vets.Select(v =>
        {
            var slots = _appointmentService.GetAvailableSlots(v.Id, hizmetId, date.Date);
            var times = string.Join(", ", slots.Select(s => s.Start.ToString("HH:mm")));
            return $"- {v.FullName} ({v.ClinicName}): {times}";
        });

        return $"{date:dd.MM.yyyy} için uygun veterinerler ve saatler:\n" + string.Join("\n", lines);
    }

    [McpServerTool(Name = "asi_takvimi")]
    [Description("Oturum açmış kullanıcının belirttiği evcil hayvanın aşı geçmişini ve yaklaşan aşılarını döndürür.")]
    public async Task<string> AsiTakvimi(
        [Description("Evcil hayvanın adı (örn. Pamuk).")] string hayvanAdi)
    {
        var userId = CurrentUserId;
        if (userId == null)
            return "Bu aracı kullanmak için oturum açmalısınız (yetkilendirme gerekli).";

        var pet = await _db.Pets
            .Include(p => p.VaccineRecords)
            .FirstOrDefaultAsync(p => p.OwnerId == userId && p.Name.ToLower() == hayvanAdi.ToLower());

        if (pet == null)
            return $"'{hayvanAdi}' adında size ait bir evcil hayvan bulunamadı.";

        var history = _petService.GetWithVaccineHistory(pet.Id);
        if (history == null)
            return "Evcil hayvan bulunamadı.";

        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"{pet.Name} ({pet.Species}{(string.IsNullOrEmpty(pet.Breed) ? "" : $" - {pet.Breed}")}) aşı takvimi:");

        if (history.Vaccines.Count == 0)
        {
            sb.AppendLine("- Kayıtlı aşı bulunmuyor.");
        }
        else
        {
            foreach (var v in history.Vaccines)
            {
                var next = v.NextDueDate.HasValue ? $" -> Sonraki doz: {v.NextDueDate:dd.MM.yyyy}" : "";
                sb.AppendLine($"- {v.VaccineName} (Yapılma: {v.AdministeredDate:dd.MM.yyyy}){next}");
            }
        }

        if (history.UpcomingVaccines.Count > 0)
        {
            sb.AppendLine("\nYaklaşan aşılar:");
            foreach (var v in history.UpcomingVaccines)
                sb.AppendLine($"* {v.VaccineName} - {v.NextDueDate:dd.MM.yyyy} ({v.DaysUntilDue} gün kaldı)");
        }

        return sb.ToString();
    }

    [McpServerTool(Name = "randevu_olustur")]
    [Description("Oturum açmış kullanıcı için 'Bekliyor' durumunda yeni bir randevu oluşturur. Uygun değilse nedenini açıklar.")]
    public async Task<string> RandevuOlustur(
        [Description("Evcil hayvanın adı (örn. Pamuk).")] string hayvanAdi,
        [Description("Hizmet ID'si.")] int hizmetId,
        [Description("Veteriner ID'si.")] int veterinerId,
        [Description("Randevu tarihi ve saati (yyyy-MM-ddTHH:mm).")] string tarih)
    {
        var userId = CurrentUserId;
        if (userId == null)
            return "Randevu oluşturmak için oturum açmalısınız (yetkilendirme gerekli).";

        if (!DateTime.TryParse(tarih, out var date))
            return $"Geçersiz tarih: '{tarih}'. Lütfen yyyy-MM-ddTHH:mm biçiminde girin.";

        var pet = await _db.Pets.FirstOrDefaultAsync(p => p.OwnerId == userId && p.Name.ToLower() == hayvanAdi.ToLower());
        if (pet == null)
            return $"'{hayvanAdi}' adında size ait bir evcil hayvan bulunamadı.";

        var result = _appointmentService.CreateAppointment(pet.Id, new CreateAppointmentRequest
        {
            PetId = pet.Id,
            ServiceId = hizmetId,
            VeterinarianId = veterinerId,
            AppointmentDate = date
        });

        if (result.Success)
            return $"Randevu oluşturuldu (Durum: Bekliyor). Randevu ID: {result.AppointmentId}. {date:dd.MM.yyyy HH:mm}";
        else
            return $"Randevu oluşturulamadı: {result.Message}";
    }
}