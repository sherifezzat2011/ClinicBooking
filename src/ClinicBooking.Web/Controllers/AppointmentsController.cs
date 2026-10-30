using ClinicBooking.Web.Data;
using ClinicBooking.Web.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Web.Controllers;

public class AppointmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AppointmentsController(ApplicationDbContext context)
    {
        _context = context;
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
        [Bind("PatientName,BirthDate,Phone,DoctorId,AppointmentDate")] CreateAppointmentViewModel model)
    {
        if (model.DoctorId.HasValue && !_context.Doctors.Any(doctor => doctor.Id == model.DoctorId.Value))
        {
            ModelState.AddModelError(nameof(model.DoctorId), "The selected doctor is not available.");
        }

        model.Doctors = GetDoctors();

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        ViewData["DetailsReviewed"] = true;

        return View(model);
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
