namespace ClinicBooking.Web.Entities;

public class Patient
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public DateOnly BirthDate { get; set; }

    public string? Phone { get; set; }

    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
}
