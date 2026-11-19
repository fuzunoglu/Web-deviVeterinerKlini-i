using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using VeterinerKlinigi.Models;

namespace VeterinerKlinigi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Clinic> Clinics => Set<Clinic>();
    public DbSet<Service> Services => Set<Service>();
    public DbSet<Veterinarian> Veterinarians => Set<Veterinarian>();
    public DbSet<VeterinarianService> VeterinarianServices => Set<VeterinarianService>();
    public DbSet<Availability> Availabilities => Set<Availability>();
    public DbSet<Pet> Pets => Set<Pet>();
    public DbSet<VaccineRecord> VaccineRecords => Set<VaccineRecord>();
    public DbSet<Appointment> Appointments => Set<Appointment>();
    public DbSet<ExaminationNote> ExaminationNotes => Set<ExaminationNote>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // VeterinarianService bileşik anahtarı.
        builder.Entity<VeterinarianService>(entity =>
        {
            entity.HasKey(vs => new { vs.VeterinarianId, vs.ServiceId });
            entity.HasOne(vs => vs.Veterinarian)
                  .WithMany(v => v.VeterinarianServices)
                  .HasForeignKey(vs => vs.VeterinarianId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(vs => vs.Service)
                  .WithMany(s => s.VeterinarianServices)
                  .HasForeignKey(vs => vs.ServiceId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Randevu silme davranışları - zincirleme silmeyi önle.
        builder.Entity<Appointment>()
               .HasOne(a => a.Pet)
               .WithMany(p => p.Appointments)
               .HasForeignKey(a => a.PetId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
               .HasOne(a => a.Service)
               .WithMany(s => s.Appointments)
               .HasForeignKey(a => a.ServiceId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
               .HasOne(a => a.Veterinarian)
               .WithMany(v => v.Appointments)
               .HasForeignKey(a => a.VeterinarianId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<Appointment>()
               .HasOne(a => a.ExaminationNote)
               .WithOne(e => e.Appointment)
               .HasForeignKey<ExaminationNote>(e => e.AppointmentId)
               .OnDelete(DeleteBehavior.Cascade);

        // Decimal hassasiyeti (SQLite için).
        builder.Entity<Service>().Property(s => s.Price).HasPrecision(18, 2);
        builder.Entity<Appointment>().Property(a => a.Price).HasPrecision(18, 2);
        builder.Entity<Pet>().Property(p => p.Weight).HasPrecision(18, 2);

        // ApplicationUser <-> Veterinarian (1-1, opsiyonel).
        builder.Entity<ApplicationUser>()
               .HasOne(u => u.Veterinarian)
               .WithOne(v => v.User)
               .HasForeignKey<ApplicationUser>(u => u.VeterinarianId)
               .OnDelete(DeleteBehavior.SetNull);
    }
}