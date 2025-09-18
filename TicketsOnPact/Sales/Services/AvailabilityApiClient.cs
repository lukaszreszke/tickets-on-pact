namespace Sales.Services;

public record ResourcesDto(IEnumerable<ResourceDto> Resources);
public record ResourceDto(int Id, string Name, string Status);

public class AvailabilityApiClient(HttpClient httpClient)
{
    public async Task<ResourcesDto?> GetAll()
    {
        var response = await httpClient.GetAsync("/api/resources");
        var content = await response.Content.ReadFromJsonAsync<ResourcesDto>();
        return content;
    }
    
    public async Task<ResourcesDto> Get(int id)
    {
        var response = await httpClient.GetAsync($"/api/resources/{id}");
        var content = await response.Content.ReadFromJsonAsync<ResourcesDto>();
        return content;
    }
}