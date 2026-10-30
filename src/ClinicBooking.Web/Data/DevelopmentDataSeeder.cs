using ClinicBooking.Web.Entities;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Web.Data;

public static class DevelopmentDataSeeder
{
    private const string ClinicName = "Demo Clinic";

    public static void Seed(ApplicationDbContext context)
    {
        var clinic = GetClinicQuery(context).SingleOrDefault();

        AddMissingData(context, clinic);

        context.SaveChanges();
    }

    private static IQueryable<Clinic> GetClinicQuery(ApplicationDbContext context)
    {
        return context.Clinics
            .Include(clinic => clinic.Doctors)
            .ThenInclude(doctor => doctor.Schedules)
            .Where(clinic => clinic.Name == ClinicName);
    }

    private static void AddMissingData(ApplicationDbContext context, Clinic? clinic)
    {
        if (clinic is null)
        {
            clinic = new Clinic
            {
                Name = ClinicName,
                Address = "Fictional demo location"
            };

            context.Clinics.Add(clinic);
        }

        AddDoctorAndSchedules(
            clinic,
            "Dr. Ahmed",
            Enum.GetValues<DayOfWeek>().Where(day => day != DayOfWeek.Friday),
            new TimeOnly(16, 0),
            new TimeOnly(20, 0));

        AddDoctorAndSchedules(
            clinic,
            "Dr. Sara",
            new[]
            {
                DayOfWeek.Sunday,
                DayOfWeek.Monday,
                DayOfWeek.Tuesday,
                DayOfWeek.Wednesday,
                DayOfWeek.Thursday
            },
            new TimeOnly(10, 0),
            new TimeOnly(14, 0));
    }

    private static void AddDoctorAndSchedules(
        Clinic clinic,
        string doctorName,
        IEnumerable<DayOfWeek> workingDays,
        TimeOnly startTime,
        TimeOnly endTime)
    {
        var doctor = clinic.Doctors.SingleOrDefault(doctor => doctor.Name == doctorName);

        if (doctor is null)
        {
            doctor = new Doctor
            {
                Name = doctorName,
                Clinic = clinic
            };

            clinic.Doctors.Add(doctor);
        }

        foreach (var day in workingDays)
        {
            // Preserve any working periods already configured for this weekday.
            if (doctor.Schedules.Any(schedule => schedule.DayOfWeek == day))
            {
                continue;
            }

            doctor.Schedules.Add(new DoctorSchedule
            {
                Doctor = doctor,
                DayOfWeek = day,
                StartTime = startTime,
                EndTime = endTime
            });
        }
    }
}
