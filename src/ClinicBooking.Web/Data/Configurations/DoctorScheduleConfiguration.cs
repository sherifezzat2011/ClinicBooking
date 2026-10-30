using ClinicBooking.Web.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ClinicBooking.Web.Data.Configurations;

public class DoctorScheduleConfiguration : IEntityTypeConfiguration<DoctorSchedule>
{
    public void Configure(EntityTypeBuilder<DoctorSchedule> builder)
    {
        builder.ToTable("DoctorSchedules", table =>
        {
            table.HasCheckConstraint(
                "CK_DoctorSchedules_DayOfWeek",
                "[DayOfWeek] BETWEEN 0 AND 6");

            table.HasCheckConstraint(
                "CK_DoctorSchedules_TimeRange",
                "[StartTime] < [EndTime]");
        });

        builder.HasKey(schedule => schedule.Id);

        builder.Property(schedule => schedule.DayOfWeek)
            .HasConversion<byte>()
            .HasColumnType("tinyint")
            .IsRequired();

        builder.Property(schedule => schedule.StartTime)
            .HasColumnType("time")
            .IsRequired();

        builder.Property(schedule => schedule.EndTime)
            .HasColumnType("time")
            .IsRequired();

        builder.HasOne(schedule => schedule.Doctor)
            .WithMany(doctor => doctor.Schedules)
            .HasForeignKey(schedule => schedule.DoctorId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(schedule => new { schedule.DoctorId, schedule.DayOfWeek });
    }
}
