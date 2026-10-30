using ClinicBooking.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Web.Data.Configurations;

public class ClinicConfiguration : IEntityTypeConfiguration<Clinic>
{
    public void Configure(EntityTypeBuilder<Clinic> builder)
    {
        builder.ToTable("Clinics");

        builder.HasKey(clinic => clinic.Id);

        builder.Property(clinic => clinic.Name)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(clinic => clinic.Address)
            .IsRequired(false)
            .HasMaxLength(250);
    }
}
