using ClinicBooking.Web.Data;
using ClinicBooking.Web.Services;
using ClinicBooking.Web.ViewModels;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Web.Controllers;

public class AppointmentsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly AppointmentAvailabilityService _availability;
    private readonly AppointmentBookingService _booking;

    public AppointmentsController(
        ApplicationDbContext context,
        AppointmentAvailabilityService availability,
        AppointmentBookingService booking)
    {
        _context = context;
        _availability = availability;
        _booking = booking;
    }

    [HttpGet]
    public IActionResult Create()
    {
        var model = new CreateAppointmentViewModel
        {
            AppointmentDate = DateOnly.FromDateTime(DateTime.Today),
            Doctors = GetDoctors()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(
        [Bind("PatientName,BirthDate,Phone,DoctorId,AppointmentDate,AppointmentTime")] CreateAppointmentViewModel model)
    {
        if (model.DoctorId.HasValue && !_context.Doctors.Any(doctor => doctor.Id == model.DoctorId.Value))
        {
            ModelState.AddModelError(nameof(model.DoctorId), "The selected doctor is not available.");
        }

        model.Doctors = GetDoctors();

        if (model.DoctorId.HasValue && model.AppointmentDate.HasValue
            && !HasFieldError(nameof(model.DoctorId)) && !HasFieldError(nameof(model.AppointmentDate)))
        {
            model.AvailableTimes = GetTimeOptions(model.DoctorId.Value, model.AppointmentDate.Value);
            if (model.AvailableTimes.Count == 0)
            {
                model.AvailabilityMessage = "No available time slots for the selected doctor and date.";
            }

            if (model.AppointmentTime.HasValue && !model.AvailableTimes.Any(option =>
                TimeOnly.ParseExact(option.Value, "HH:mm", CultureInfo.InvariantCulture) == model.AppointmentTime.Value))
            {
                ModelState.AddModelError(nameof(model.AppointmentTime),
                    "The selected time is no longer available. Select another time.");
            }
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var result = _booking.Book(
            model.PatientName, model.BirthDate!.Value, model.Phone,
            model.DoctorId!.Value, model.AppointmentDate!.Value, model.AppointmentTime!.Value);

        if (result.Success)
        {
            TempData["SuccessMessage"] = $"Appointment #{result.AppointmentId} saved successfully.";
            return RedirectToAction(nameof(Create));
        }

        ModelState.AddModelError(string.Empty, result.Message);
        model.AvailableTimes = GetTimeOptions(model.DoctorId.Value, model.AppointmentDate.Value);

        if (!model.AvailableTimes.Any(option =>
            TimeOnly.ParseExact(option.Value, "HH:mm", CultureInfo.InvariantCulture) == model.AppointmentTime.Value))
        {
            ModelState.Remove(nameof(model.AppointmentTime));
            model.AppointmentTime = null;
        }

        if (model.AvailableTimes.Count == 0)
        {
            model.AvailabilityMessage = "No available time slots for the selected doctor and date.";
        }

        return View(model);
    }

    [HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult AvailableSlots(int? doctorId, DateOnly? date)
    {
        if (!ModelState.IsValid || !doctorId.HasValue || doctorId.Value <= 0 || !date.HasValue)
        {
            return BadRequest(new { message = "Select a valid doctor and appointment date." });
        }

        if (date.Value < DateOnly.FromDateTime(DateTime.Today))
        {
            return BadRequest(new { message = "Appointment date cannot be in the past." });
        }

        if (!_context.Doctors.Any(doctor => doctor.Id == doctorId.Value))
        {
            return NotFound(new { message = "The selected doctor is not available." });
        }

        var slots = GetTimeOptions(doctorId.Value, date.Value);
        return Json(new
        {
            slots = slots.Select(slot => new { value = slot.Value, text = slot.Text }),
            message = slots.Count == 0
                ? "No available time slots for the selected doctor and date."
                : string.Empty
        });
    }

    private bool HasFieldError(string field)
    {
        return ModelState.TryGetValue(field, out var entry) && entry.Errors.Count > 0;
    }

    private List<SelectListItem> GetTimeOptions(int doctorId, DateOnly date)
    {
        return _availability.GetAvailableSlots(doctorId, date)
            .Select(time =>
            {
                var start = time.ToString("HH:mm", CultureInfo.InvariantCulture);
                var end = time.AddMinutes(AppointmentAvailabilityService.VisitLengthMinutes)
                    .ToString("HH:mm", CultureInfo.InvariantCulture);

                return new SelectListItem { Value = start, Text = $"{start} - {end}" };
            })
            .ToList();
    }

    private List<SelectListItem> GetDoctors()
    {
        return _context.Doctors
            .AsNoTracking()
            .OrderBy(doctor => doctor.Name)
            .Select(doctor => new SelectListItem
            {
                Value = doctor.Id.ToString(),
                Text = doctor.Name
            })
            .ToList();
    }
}
