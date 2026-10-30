namespace ClinicBooking.Web.ViewModels;

public class AppointmentListItemViewModel
{
    public int Id { get; set; }

    public required string PatientName { get; set; }

    public required string DoctorName { get; set; }

    public DateTime StartDateTime { get; set; }

    public DateTime EndDateTime { get; set; }
}
