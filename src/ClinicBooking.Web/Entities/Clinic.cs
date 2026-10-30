namespace ClinicBooking.Web.Entities;

public class Clinic
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Address { get; set; }

    public ICollection<Doctor> Doctors { get; set; } = new List<Doctor>();
}
