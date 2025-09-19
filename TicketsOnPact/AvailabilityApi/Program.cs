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
            var options = new DbContextOptionsBuilder<AvailabilityContext>()
                .UseInMemoryDatabase("AvailabilityTest")
                .Options;

            builder.Services.AddSingleton(options);
            builder.Services.AddScoped<AvailabilityContext>(sp =>
                new AvailabilityContext(options));
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
        
        app.MapGet("/api/resources", (AvailabilityContext context) =>
        {
            return Results.Ok(new { Resources = context.Resources.ToArray() });
        });

        app.MapGet("/api/resources/{id}", (int id, AvailabilityContext context) =>
        {
            var resource = context.Resources.FirstOrDefault(x => x.Id == id);
            return resource == null ? Results.NotFound() : Results.Ok(new { resource.Id, resource.Name, resource.Status });
        });

        app.MapPost("/api/resources/{id}", (int id, AvailabilityContext context, [FromBody] dynamic json) =>
        {
            var resource = context.Resources.FirstOrDefault(x => x.Id == id);
            if (resource == null)
                return Results.NotFound();

            resource.Status = "blocked";
            context.SaveChanges();
            
            return Results.Ok(new { resource.Id, resource.Status });
        });

        app.MapHealthChecks("/health");

        logger.LogInformation("Application configured and ready to start");
        return app;
    }
}