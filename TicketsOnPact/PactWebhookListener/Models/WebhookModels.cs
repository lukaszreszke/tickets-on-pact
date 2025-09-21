namespace PactWebhookListener.Models;

public class PactWebhookPayload
{
    public Consumer Consumer { get; set; } = new();
    public Provider Provider { get; set; } = new();
    public string PactUrl { get; set; } = string.Empty;
    public string Event { get; set; } = string.Empty;
    public string? VerificationResultUrl { get; set; }
    public string? ConsumerVersion { get; set; }
    public string? ProviderVersion { get; set; }
}

public class Consumer
{
    public string Name { get; set; } = string.Empty;
}

public class Provider
{
    public string Name { get; set; } = string.Empty;
}
