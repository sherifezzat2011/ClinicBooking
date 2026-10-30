using ClinicBooking.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Web.Data.Configurations;

public class PatientConfiguration : IEntityTypeConfiguration<Patient>
{
    public void Configure(EntityTypeBuilder<Patient> builder)
    {
        builder.ToTable("Patients");

        builder.HasKey(patient => patient.Id);

        builder.Property(patient => patient.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(patient => patient.BirthDate)
            .HasColumnType("date")
            .IsRequired();

        builder.Property(patient => patient.Phone)
            .IsRequired(false)
            .HasMaxLength(30);
    }
}
