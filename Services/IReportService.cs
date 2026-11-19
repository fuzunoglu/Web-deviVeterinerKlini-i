namespace VeterinerKlinigi.Services;

public class ServiceReportRow
{
    public int ServiceId { get; set; }
    public string ServiceName { get; set; } = string.Empty;
    public int AppointmentCount { get; set; }
    public decimal Revenue { get; set; }
}

public interface IReportService
{
    /// <summary>Her hizmet türü için randevu sayısı ve ciro (LINQ).</summary>
    List<ServiceReportRow> GetServiceReport(DateTime? from = null, DateTime? to = null);
}

public class ReportService : IReportService
{
    private readonly Data.ApplicationDbContext _db;

    public ReportService(Data.ApplicationDbContext db)
    {
        _db = db;
    }

    public List<ServiceReportRow> GetServiceReport(DateTime? from = null, DateTime? to = null)
    {
        var query = _db.Appointments.AsQueryable();

        // Yalnızca tamamlanmış veya onaylanmış randevular ciroya dahil edilir.
        query = query.Where(a => a.Status == Models.AppointmentStatus.Tamamlandi
                              || a.Status == Models.AppointmentStatus.Onaylandi);

        if (from.HasValue) query = query.Where(a => a.AppointmentDate >= from.Value);
        if (to.HasValue) query = query.Where(a => a.AppointmentDate <= to.Value);

        return query
            .GroupBy(a => new { a.ServiceId, a.Service!.Name })
            .Select(g => new ServiceReportRow
            {
                ServiceId = g.Key.ServiceId,
                ServiceName = g.Key.Name,
                AppointmentCount = g.Count(),
                Revenue = g.Sum(a => a.Price)
            })
            .OrderByDescending(r => r.Revenue)
            .ToList();
    }
}