using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using PactWebhookListener.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower;
    options.SerializerOptions.PropertyNameCaseInsensitive = true;
});

var app = builder.Build();

app.MapPost("/webhook", async (HttpContext context) =>
{
    try
    {
        context.Request.EnableBuffering();
        var body = await new StreamReader(context.Request.Body).ReadToEndAsync();
        context.Request.Body.Position = 0;

        var payload = await JsonSerializer.DeserializeAsync<PactWebhookPayload>(
            context.Request.Body,
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                PropertyNameCaseInsensitive = true
            }
        );

        if (payload != null)
        {
            Console.WriteLine("=== Pact Broker Webhook Received ===");
            Console.WriteLine($"Consumer: {payload.Consumer.Name}");
            Console.WriteLine($"Provider: {payload.Provider.Name}");
            Console.WriteLine($"Pact URL: {payload.PactUrl}");
            Console.WriteLine($"Event: {payload.Event}");

            if (!string.IsNullOrEmpty(payload.ConsumerVersion))
            {
                Console.WriteLine($"Consumer Version: {payload.ConsumerVersion}");
            }

            if (!string.IsNullOrEmpty(payload.ProviderVersion))
            {
                Console.WriteLine($"Provider Version: {payload.ProviderVersion}");
            }

            if (!string.IsNullOrEmpty(payload.VerificationResultUrl))
            {
                Console.WriteLine($"Verification Result URL: {payload.VerificationResultUrl}");
            }

            Console.WriteLine("Full body:");
            Console.WriteLine(body);
            Console.WriteLine("=====================================");
            Console.WriteLine();
        }
        else
        {
            Console.WriteLine("Failed to parse webhook payload");
            Console.WriteLine($"Raw body: {body}");
        }

        return Results.Ok(new { status = "received" });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error processing webhook: {ex.Message}");
        return Results.BadRequest(new { error = ex.Message });
    }
});

app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

app.Urls.Add("http://0.0.0.0:6000");
app.Urls.Add("https://0.0.0.0:6001");

Console.WriteLine("Pact Webhook Listener is starting...");
Console.WriteLine("Listening on:");
Console.WriteLine("  - https://localhost:6001/webhook (HTTPS - for Pact Broker)");
Console.WriteLine("  - https://host.docker.internal:6001/webhook (HTTPS - from Docker)");
Console.WriteLine("  - http://localhost:6000/webhook (HTTP - fallback)");
Console.WriteLine("Health check available at: https://localhost:6001/health");
Console.WriteLine("");
Console.WriteLine("Note: If you get SSL certificate errors, run:");
Console.WriteLine("  dotnet dev-certs https --trust");
Console.WriteLine("Press Ctrl+C to stop");

await app.RunAsync();
