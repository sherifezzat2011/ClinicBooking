namespace ClinicBooking.Web.Entities;

public class Doctor
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Phone { get; set; }

    public int ClinicId { get; set; }

    public Clinic Clinic { get; set; } = null!;

    public ICollection<DoctorSchedule> Schedules { get; set; } = new List<DoctorSchedule>();

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
