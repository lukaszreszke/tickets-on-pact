namespace Sales.Services;

public class HealthCheckClient(HttpClient httpClient)
{
    public async Task<string> DudeYouOk()
    {
        var response = await httpClient.GetAsync("health");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
}