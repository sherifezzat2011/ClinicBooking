namespace ClinicBooking.Web.Services;

public record BookingResult(bool Success, string Message, int? AppointmentId = null);
