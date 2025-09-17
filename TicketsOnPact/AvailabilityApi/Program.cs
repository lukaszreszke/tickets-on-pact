using AvailabilityApi.Infrastructure;
using AvailabilityApi.Models;
using AvailabilityApi.Services;
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
   //     builder.Services.AddHostedService<HelloRabbitListener>();

        builder.Services.AddDbContextPool<AvailabilityContext>(opt =>
        {
            opt.UseNpgsql(builder.Configuration.GetConnectionString("Availability"));
            opt.EnableSensitiveDataLogging();
        });
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
        
        app.MapGet("/resource/create",
            (AvailabilityContext context) =>
            {
                var resource = context.Resources.Add(new Resource() { Status = "Available" });
                context.SaveChanges();
                return Results.Ok(resource.Entity.Id);
            });

        app.MapGet("/resource/{id}", (int id, AvailabilityContext context) =>
        {
            var resource = context.Resources.FirstOrDefault(x => x.Id == id);
            return resource != null ? Results.Ok(new { resource.Id, ResourceState = resource.Status, resource.Status, ValidUntilThisDate = DateTime.UtcNow }) : Results.NotFound();
        });

        app.MapGet("/block/{id}", (int id) =>
        {
            return id switch
            {
                1 => Results.Ok(new { Result = "Blocked" }),
                2 => Results.BadRequest("Resource cannot be blocked"),
                _ => Results.NotFound($"Resource with ID {id} not found")
            };
        });

        app.MapGet("/resources", (AvailabilityContext context) =>
        {
            var resources = context.Resources.ToList();
            return Results.Ok(resources);
        });

        app.MapPost("block/{id}", (int id, AvailabilityContext context) =>
        {
            context.Resources.FirstOrDefault(x => x.Id == id)!.Status = $"Blocked";
            context.SaveChanges();
        });
        
        app.MapPut("unblock/{id}", (int id, AvailabilityContext context) =>
        {
            context.Resources.FirstOrDefault(x => x.Id == id)!.Status = $"Unblocked";
            context.SaveChanges();
        });

        app.MapGet("blocked", (AvailabilityContext context) =>
        {
            var resource = context.Resources.Where(x => x.Status == "Blocked").FirstOrDefault();
            return Results.Ok(new { resource.Id, resource.Status });
        });

        app.MapHealthChecks("/health");
 
        logger.LogInformation("Application configured and ready to start");
        return app;
    }
}
