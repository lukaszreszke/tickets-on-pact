using AvailabilityApi.Models;

namespace AvailabilityApi.Dtos;

public class BlockedResource
{
    public int Id { get; set; }
    public string Status { get; set; }
    
    public static BlockedResource FromResource(Resource resource)
    {
        return new BlockedResource() { Id = resource.Id, Status = resource.Status };
    }
}