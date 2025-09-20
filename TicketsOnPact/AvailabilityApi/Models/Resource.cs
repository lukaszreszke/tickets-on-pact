namespace AvailabilityApi.Models;

public class Resource
{
    public int Id { get; set; }
    public string Status { get; set; }
    public string Name { get; set; }
    public DateTime BlockedUntil { get; set; }
}