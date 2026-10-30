using ClinicBooking.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Web.Data.Configurations;

public class DoctorConfiguration : IEntityTypeConfiguration<Doctor>
{
    public void Configure(EntityTypeBuilder<Doctor> builder)
    {
        builder.ToTable("Doctors");

        builder.HasKey(doctor => doctor.Id);

        builder.Property(doctor => doctor.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(doctor => doctor.Phone)
            .IsRequired(false)
            .HasMaxLength(30);

        builder.HasOne(doctor => doctor.Clinic)
            .WithMany(clinic => clinic.Doctors)
            .HasForeignKey(doctor => doctor.ClinicId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);
    }
}
