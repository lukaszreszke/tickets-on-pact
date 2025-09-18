using AvailabilityApi.Infrastructure;
using AvailabilityApi.Models;
using AvailabilityApi.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

var app = Program.BuildApp(args);
app.Run();

public partial class Program
{
    public static WebApplication BuildApp(string[]? args = null)
    {
        args ??= Array.Empty<string>();
        var builder = WebApplication.CreateBuilder(args);

        builder.Logging.ClearProviders();
        builder.Logging.AddConsole();
        builder.Logging.SetMinimumLevel(LogLevel.Information);
        builder.Services.AddOpenApi();
        builder.Services.AddHealthChecks();

        if (builder.Environment.IsEnvironment("Testing"))
        {
           builder.Services.AddDbContextPool<AvailabilityContext>(o => o.UseInMemoryDatabase("AvailabilityTest"));
        }
        else
        {
           builder.Services.AddDbContextPool<AvailabilityContext>(o =>
           {
               o.UseNpgsql(builder.Configuration.GetConnectionString("Availability"));
               o.EnableSensitiveDataLogging();
           });
        }
        var app = builder.Build();

        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogInformation("Starting Availability application...");

        using (var scope = app.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AvailabilityContext>();
            dbContext.Database.EnsureCreated();
        }

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            logger.LogInformation("Development environment detected - OpenAPI enabled");
        }

        app.UseHttpsRedirection();

        app.MapGet("/health-db", (ILogger<Program> logger) =>
        {
            try
            {
                using var conn =
                    new NpgsqlConnection(builder.Configuration.GetConnectionString("Availability"));
                conn.Open();
                using var cmd = new NpgsqlCommand("SELECT 1", conn);
                cmd.ExecuteScalar();
                return Results.Ok("DB is alive");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Database health check failed");
                return Results.Problem("Database connection failed");
            }
        });

        app.MapGet("/api/resources", () =>
        {
            return new
            {
                Resources = new[]
                {
                    new { id = 1, status = "available", name = "LadyGaGa" },
                    new { id = 2, status = "blocked", name = "T-Love" },
                    new { id = 3, status = "temporary_blocked", name = "Snoop Dog" }
                }
            };
        });

        app.MapGet("/api/resources/{id}", (int id) =>
        {
            if(id == 1)
                return Results.Ok(new { id = 1, status = "available", name = "LadyGaGa" });
            return Results.NotFound();
        });

        app.MapPost("/api/resources/{id}", (int id, [FromBody] dynamic json) =>
        {
            if (id == 1)
            {
                return Results.Ok(new { id = 1, status = "blocked" });
            }
            return Results.NotFound();
        });

        app.MapHealthChecks("/health");

        logger.LogInformation("Application configured and ready to start");
        return app;
    }
}