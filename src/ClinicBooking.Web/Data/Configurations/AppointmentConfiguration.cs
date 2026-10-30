using ClinicBooking.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Web.Data.Configurations;

public class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments", table =>
        {
            table.HasCheckConstraint(
                "CK_Appointments_DateTimeRange",
                "[StartDateTime] < [EndDateTime]");
        });

        builder.HasKey(appointment => appointment.Id);

        builder.Property(appointment => appointment.StartDateTime)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(appointment => appointment.EndDateTime)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.HasOne(appointment => appointment.Doctor)
            .WithMany(doctor => doctor.Appointments)
            .HasForeignKey(appointment => appointment.DoctorId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(appointment => appointment.Patient)
            .WithMany(patient => patient.Appointments)
            .HasForeignKey(appointment => appointment.PatientId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(appointment => new
        {
            appointment.DoctorId,
            appointment.StartDateTime
        });
    }
}
