using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ClinicBooking.Web.ViewModels;

public class CreateAppointmentViewModel : IValidatableObject
{
    [Required(ErrorMessage = "Enter the patient's name.")]
    [StringLength(150)]
    [Display(Name = "Patient name")]
    public string PatientName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Enter the patient's birth date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Birth date")]
    public DateOnly? BirthDate { get; set; }

    [StringLength(30)]
    [Display(Name = "Phone number")]
    public string? Phone { get; set; }

    [Required(ErrorMessage = "Select a doctor.")]
    [Range(1, int.MaxValue, ErrorMessage = "Select a valid doctor.")]
    [Display(Name = "Doctor")]
    public int? DoctorId { get; set; }

    [Required(ErrorMessage = "Select an appointment date.")]
    [DataType(DataType.Date)]
    [Display(Name = "Appointment date")]
    public DateOnly? AppointmentDate { get; set; }

    [Required(ErrorMessage = "Select an available appointment time.")]
    [Display(Name = "Appointment time")]
    public TimeOnly? AppointmentTime { get; set; }

    public List<SelectListItem> Doctors { get; set; } = new();

    public List<SelectListItem> AvailableTimes { get; set; } = new();

    public string? AvailabilityMessage { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (BirthDate.HasValue && BirthDate.Value > today)
        {
            yield return new ValidationResult(
                "Birth date cannot be in the future.",
                new[] { nameof(BirthDate) });
        }

        if (AppointmentDate.HasValue && AppointmentDate.Value < today)
        {
            yield return new ValidationResult(
                "Appointment date cannot be in the past.",
                new[] { nameof(AppointmentDate) });
        }
    }
}
