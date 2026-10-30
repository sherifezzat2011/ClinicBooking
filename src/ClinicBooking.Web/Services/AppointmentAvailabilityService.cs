using ClinicBooking.Web.Data;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Web.Services;

public class AppointmentAvailabilityService
{
    public const int VisitLengthMinutes = 30;

    private readonly ApplicationDbContext _context;
    private readonly TimeProvider _clock;

    public AppointmentAvailabilityService(ApplicationDbContext context, TimeProvider clock)
    {
        _context = context;
        _clock = clock;
    }

    public List<TimeOnly> GetAvailableSlots(int doctorId, DateOnly date)
    {
        var now = _clock.GetLocalNow().DateTime;
        if (date < DateOnly.FromDateTime(now))
        {
            return new List<TimeOnly>();
        }

        var schedules = _context.DoctorSchedules.AsNoTracking()
            .Where(schedule => schedule.DoctorId == doctorId && schedule.DayOfWeek == date.DayOfWeek)
            .ToList();

        if (schedules.Count == 0)
        {
            return new List<TimeOnly>();
        }

        var workingStart = date.ToDateTime(schedules.Min(schedule => schedule.StartTime));
        var workingEnd = date.ToDateTime(schedules.Max(schedule => schedule.EndTime));
        var appointments = _context.Appointments.AsNoTracking()
            .Where(appointment => appointment.DoctorId == doctorId
                && appointment.StartDateTime < workingEnd
                && appointment.EndDateTime > workingStart)
            .Select(appointment => new { appointment.StartDateTime, appointment.EndDateTime })
            .ToList();

        var slots = new HashSet<TimeOnly>();

        foreach (var schedule in schedules)
        {
            var end = date.ToDateTime(schedule.EndTime);
            for (var start = date.ToDateTime(schedule.StartTime);
                start <= end.AddMinutes(-VisitLengthMinutes);
                start = start.AddMinutes(VisitLengthMinutes))
            {
                var slotEnd = start.AddMinutes(VisitLengthMinutes);
                if (start <= now || appointments.Any(appointment =>
                    appointment.StartDateTime < slotEnd && appointment.EndDateTime > start))
                {
                    continue;
                }

                slots.Add(TimeOnly.FromDateTime(start));
            }
        }

        return slots.OrderBy(time => time).ToList();
    }
}
