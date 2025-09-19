using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Moq;
using PactNet;
using PactNet.Output.Xunit;
using Sales.Services;
using Xunit.Abstractions;

namespace SalesTests;

public class AvailabilityConsumerTests
{
    private readonly IPactBuilderV4 _pact;
    private readonly Mock<IHttpClientFactory> _mockFactory;

    public AvailabilityConsumerTests(ITestOutputHelper output)
    {
        _mockFactory = new Mock<IHttpClientFactory>();

        var config = new PactConfig
        {
            Outputters = new[] { new XunitOutput(output) },
            DefaultJsonSettings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true,
                Converters = { new JsonStringEnumConverter() }
            },
            LogLevel = PactLogLevel.Information
        };

        _pact = Pact.V4("Sales", "AvailabilityApi", config).WithHttpInteractions();
    }

    [Fact]
    public async Task TestHealthCheck()
    {
        _pact.UponReceiving("healthcheck request")
            .WithRequest(HttpMethod.Get, "/health")
            .WillRespond()
            .WithBody("Healthy", "text/plain")
            .WithStatus(HttpStatusCode.OK);

        await _pact.VerifyAsync(async ctx =>
        {
            _mockFactory.Setup(x => x.CreateClient("AvailabilityApi"))
                .Returns(() => new HttpClient
                {
                    BaseAddress = ctx.MockServerUri,
                    DefaultRequestHeaders =
                    {
                        Accept = { MediaTypeWithQualityHeaderValue.Parse("application/json") }
                    }
                });

            var healthCheckClient = new HealthCheckClient(_mockFactory.Object.CreateClient("AvailabilityApi"));
            var result = await healthCheckClient.DudeYouOk();
            Assert.Equal("Healthy", result);
        });
    }

    [Fact]
    public async Task GetAllResources()
    {
        _pact.UponReceiving("get all resources")
            .Given("all resources")
            .WithRequest(HttpMethod.Get, "/api/resources")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new
            {
                resources = new[]
                {
                    new { id = 1, status = "available", name = "LadyGaGa" },
                    new { id = 2, status = "blocked", name = "T-Love" },
                    new { id = 3, status = "temporary_blocked", name = "Snoop Dog" }
                }
            });

        await _pact.VerifyAsync(async ctx =>
        {
            _mockFactory.Setup(x => x.CreateClient("AvailabilityApi"))
                .Returns(() => new HttpClient
                {
                    BaseAddress = ctx.MockServerUri,
                    DefaultRequestHeaders =
                    {
                        Accept = { MediaTypeWithQualityHeaderValue.Parse("application/json") }
                    }
                });

            var client = new AvailabilityApiClient(_mockFactory.Object.CreateClient("AvailabilityApi"));
            var result = await client.GetAll();
            Assert.NotNull(result);
        });
    }

    [Fact]
    public async Task GetResource()
    {
        _pact.UponReceiving("a request to get resource by id")
            .Given("resource with ID {id} exists", new Dictionary<string, string> { ["id"] = "1" })
            .WithRequest(HttpMethod.Get, "/api/resources/1")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new ResourceDto(1, "LadyGaGa", "available"));

        await _pact.VerifyAsync(async ctx =>
        {
            _mockFactory.Setup(x => x.CreateClient("AvailabilityApi"))
                .Returns(() => new HttpClient
                {
                    BaseAddress = ctx.MockServerUri,
                    DefaultRequestHeaders =
                    {
                        Accept = { MediaTypeWithQualityHeaderValue.Parse("application/json") }
                    }
                });

            var client = new AvailabilityApiClient(_mockFactory.Object.CreateClient("AvailabilityApi"));
            var result = await client.Get(1);
            Assert.NotNull(result);
        });
    }
    
    
    [Fact]
    public async Task BlockResource()
    {
        _pact.UponReceiving("a request to block specific resource")
            .WithRequest(HttpMethod.Post, "/api/resources/1")
            .WithJsonBody(new { id = 1 })
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new { id = 1, status = "blocked" });

        await _pact.VerifyAsync(async ctx =>
        {
            _mockFactory.Setup(x => x.CreateClient("AvailabilityApi"))
                .Returns(() => new HttpClient
                {
                    BaseAddress = ctx.MockServerUri,
                    DefaultRequestHeaders =
                    {
                        Accept = { MediaTypeWithQualityHeaderValue.Parse("application/json") }
                    }
                });

            var client = new AvailabilityApiClient(_mockFactory.Object.CreateClient("AvailabilityApi"));
            var result = await client.Block(1);
            Assert.NotNull(result);
        });
    }
}