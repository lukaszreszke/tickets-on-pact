using System.Text;
using System.Text.Json;
using AvailabilityApi.Infrastructure;
using AvailabilityApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace AvailabilityApiTests
{
    /// <summary>
    /// Middleware for handling provider state requests
    /// </summary>
    public class ProviderStateMiddleware
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private readonly IDictionary<string, Action<AvailabilityContext>> providerStates;
        private readonly RequestDelegate next;

        /// <summary>
        /// Initialises a new instance of the <see cref="ProviderStateMiddleware"/> class.
        /// </summary>
        /// <param name="next">Next request delegate</param>
        /// <param name="serviceScopeFactory">Service scope factory for creating database contexts</param>
        public ProviderStateMiddleware(RequestDelegate next, IServiceScopeFactory serviceScopeFactory)
        {
            this.next = next;
            _serviceScopeFactory = serviceScopeFactory;

            this.providerStates = new Dictionary<string, Action<AvailabilityContext>>
            {
                ["all resources"] = context =>
                {
                    context.RemoveRange(context.Resources);
                    context.SaveChanges();
                    var resources = new List<Resource>
                    {
                        new() { Id = 1, Status = "available", Name = "LadyGaGa" },
                    };
                    context.AddRange(resources);
                    context.SaveChanges();
                },
                ["resource with ID {id} exists"] = context =>
                {
                    var resource = context.Resources.FirstOrDefault(x => x.Id == 1);
                    if (resource == null)
                    {
                        context.Resources.Add(new Resource { Id = 1, Status = "available", Name = "LadyGaGa" });
                        context.SaveChanges();
                    }
                },
                ["there are blocked resources"] = context =>
                {
                    context.RemoveRange(context.Resources);
                    context.SaveChanges();
                    var resources = new List<Resource>
                    {
                        new() { Id = 1, Status = "blocked", Name = "LadyGaGa" },
                        new() { Id = 2, Status = "blocked", Name = "T-Love" },
                        new() { Id = 3, Status = "blocked", Name = "Snoop Dog"}
                    };
                    context.AddRange(resources);
                    context.SaveChanges();
                },
                ["there are temporary blocked resources"] = context =>
                {
                    context.RemoveRange(context.Resources);
                    context.SaveChanges();
                    var resources = new List<Resource>
                    {
                        new() { Id = 1, BlockedUntil = DateTime.Parse("2024-12-31T23:59:59Z"), Status = "temporary_blocked", Name = "Snoop Dog" },
                        new() { Id = 2, BlockedUntil = DateTime.Parse("2024-12-30T15:30:00Z"),  Status = "temporary_blocked", Name = "T-Love" },
                        new() { Id = 3, BlockedUntil = DateTime.Parse("2024-12-29T10:00:00Z"),   Status = "temporary_blocked", Name = "Snoop Dog" }
                    };
                    context.AddRange(resources);
                    context.SaveChanges();
                }
            };
        }

        /// <summary>
        /// Handle the request
        /// </summary>
        /// <param name="context">Request context</param>
        /// <returns>Awaitable</returns>
        public async Task InvokeAsync(HttpContext context)
        {
            if (!(context.Request.Path.Value?.StartsWith("/provider-states") ?? false))
            {
                await this.next.Invoke(context);
                return;
            }

            context.Response.StatusCode = StatusCodes.Status200OK;

            if (context.Request.Method == HttpMethod.Post.ToString())
            {
                string jsonRequestBody;

                using (var reader = new StreamReader(context.Request.Body, Encoding.UTF8))
                {
                    jsonRequestBody = await reader.ReadToEndAsync();
                }

                try
                {
                    ProviderState providerState = JsonSerializer.Deserialize<ProviderState>(jsonRequestBody, Options);

                    if (!string.IsNullOrEmpty(providerState?.State) &&
                        this.providerStates.ContainsKey(providerState.State))
                    {
                        using var scope = _serviceScopeFactory.CreateScope();
                        var dbContext = scope.ServiceProvider.GetRequiredService<AvailabilityContext>();

                        this.providerStates[providerState.State].Invoke(dbContext);
                    }
                }
                catch (Exception e)
                {
                    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                    await context.Response.WriteAsync("Failed to deserialise JSON provider state body:");
                    await context.Response.WriteAsync(jsonRequestBody);
                    await context.Response.WriteAsync(string.Empty);
                    await context.Response.WriteAsync(e.ToString());
                }
            }
        }
    }
}
