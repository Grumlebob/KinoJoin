namespace Application.DTO;

//Not a record, because we want mutability when changing the details.
public class HostJoinEventDetails
{
    public string? EventDescription { get; set; }
    public string? EventTitle { get; set; }
    public DateTime Deadline { get; set; }
}
