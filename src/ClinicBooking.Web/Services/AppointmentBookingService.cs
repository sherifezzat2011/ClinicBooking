using ClinicBooking.Web.Data;
using ClinicBooking.Web.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Web.Services;

public class AppointmentBookingService
{
    private readonly ApplicationDbContext _context;
    private readonly AppointmentAvailabilityService _availability;
    private readonly TimeProvider _clock;
    private readonly ILogger<AppointmentBookingService> _logger;

    public AppointmentBookingService(
        ApplicationDbContext context,
        AppointmentAvailabilityService availability,
        TimeProvider clock,
        ILogger<AppointmentBookingService> logger)
    {
        _context = context;
        _availability = availability;
        _clock = clock;
        _logger = logger;
    }

    public BookingResult Book(
        string patientName, DateOnly birthDate, string? phone,
        int doctorId, DateOnly date, TimeOnly time)
    {
        var today = DateOnly.FromDateTime(_clock.GetLocalNow().DateTime);
        if (string.IsNullOrWhiteSpace(patientName) || patientName.Trim().Length > 150
            || phone?.Trim().Length > 30 || birthDate > today || doctorId <= 0)
        {
            return new BookingResult(false, "Check the patient and doctor details.");
        }

        try
        {
            using var transaction = _context.Database.BeginTransaction();

            // Serialize booking attempts for this doctor across application instances.
            // SQL Server releases the lock when this transaction commits or rolls back.
            var resource = $"ClinicBooking:Doctor:{doctorId}";
            _context.Database.ExecuteSqlInterpolated($"""
                DECLARE @result int;
                EXEC @result = sys.sp_getapplock
                    @Resource = {resource},
                    @LockMode = 'Exclusive',
                    @LockOwner = 'Transaction',
                    @LockTimeout = 10000;
                IF @result < 0
                    THROW 51001, 'Could not acquire the doctor booking lock.', 1;
                """);

            if (!_context.Doctors.Any(doctor => doctor.Id == doctorId))
            {
                return new BookingResult(false, "The selected doctor is not available.");
            }

            if (!_availability.GetAvailableSlots(doctorId, date).Contains(time))
            {
                return new BookingResult(false,
                    "The selected time is no longer available. Select another time.");
            }

            var start = date.ToDateTime(time);
            var appointment = new Appointment
            {
                DoctorId = doctorId,
                Patient = new Patient
                {
                    Name = patientName.Trim(),
                    BirthDate = birthDate,
                    Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim()
                },
                StartDateTime = start,
                EndDateTime = start.AddMinutes(AppointmentAvailabilityService.VisitLengthMinutes)
            };

            _context.Appointments.Add(appointment);
            _context.SaveChanges();
            transaction.Commit();

            return new BookingResult(true, "Appointment saved successfully.", appointment.Id);
        }
        catch (SqlException exception) when (exception.Number is 51001 or 1205 or 1222)
        {
            _logger.LogWarning(exception, "Booking lock failed for doctor {DoctorId}.", doctorId);
            _context.ChangeTracker.Clear();
            return new BookingResult(false, "Another booking is being processed. Please select an available time and try again.");
        }
        catch (Exception exception) when (exception is DbUpdateException or SqlException)
        {
            _logger.LogError(exception, "Could not save an appointment for doctor {DoctorId}.", doctorId);
            _context.ChangeTracker.Clear();
            return new BookingResult(false, "The appointment could not be saved. Please try again.");
        }
    }
}
