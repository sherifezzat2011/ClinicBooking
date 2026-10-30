using System.Diagnostics;
using ClinicBooking.Web.Data;
using Microsoft.AspNetCore.Mvc;
using ClinicBooking.Web.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ClinicBooking.Web.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public IActionResult Index()
    {
        var appointments = _context.Appointments
            .AsNoTracking()
            .OrderBy(appointment => appointment.StartDateTime)
            .ThenBy(appointment => appointment.Id)
            .Select(appointment => new AppointmentListItemViewModel
            {
                Id = appointment.Id,
                PatientName = appointment.Patient.Name,
                DoctorName = appointment.Doctor.Name,
                StartDateTime = appointment.StartDateTime,
                EndDateTime = appointment.EndDateTime
            })
            .ToList();

        return View(appointments);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
