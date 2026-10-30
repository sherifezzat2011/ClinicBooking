using ClinicBooking.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Clinic> Clinics => Set<Clinic>();

    public DbSet<Doctor> Doctors => Set<Doctor>();

    public DbSet<DoctorSchedule> DoctorSchedules => Set<DoctorSchedule>();

    public DbSet<Patient> Patients => Set<Patient>();

    public DbSet<Appointment> Appointments => Set<Appointment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
