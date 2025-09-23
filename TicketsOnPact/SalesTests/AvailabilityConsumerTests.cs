using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using Moq;
using PactNet;
using PactNet.Output.Xunit;
using Sales.Services;
using Xunit.Abstractions;
using Match = PactNet.Matchers.Match;

namespace SalesTests;

//[Collection(nameof(AvailabilityConsumerTestCollection))]
public class AvailabilityConsumerTests
{
    private readonly IPactBuilderV4 _pact;
    private readonly Mock<IHttpClientFactory> _mockFactory;
    private readonly AvailabilityConsumerFixture _fixture;

    public AvailabilityConsumerTests(ITestOutputHelper output)
    {
        _mockFactory = new Mock<IHttpClientFactory>();
        _fixture = new  AvailabilityConsumerFixture(output);
        _pact = _fixture.PactBuilder;
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
                resources =  new []{ 
                    new { id = Match.Integer(1), status = Match.Regex("available", "available|blocked|temporary_blocked"), name = Match.Regex("LadyGaGa", ".*") },
                    new { id = Match.Integer(2), status = Match.Regex("blocked", "available|blocked|temporary_blocked"), name = Match.Regex("T-Love", ".*") },
                    new { id = Match.Integer(3), status = Match.Regex("temporary_blocked", "available|blocked|temporary_blocked"), name = Match.Regex("Snoop Dog", ".*") }
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
        _fixture.GivenResourceExists();
        
        await _pact.VerifyAsync(async ctx =>
        {
            var client = NewClient(ctx);
            await client.Get(1);
        });
    }

    private AvailabilityApiClient NewClient(IConsumerContext ctx)
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
        return client;
    }
    
    [Fact]
    public async Task BlockResource()
    {
        _pact.UponReceiving("a request to block specific resource")
            .Given("resource with ID {id} exists", new Dictionary<string, string> { ["id"] = "1" })
            .WithRequest(HttpMethod.Post, "/api/resources/1")
            .WithJsonBody(new { id = 1 })
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new { id = Match.Integer(1), status = Match.Regex("available", "available|blocked") });

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
    
    [Fact]
    public async Task GetAllBlockedResources()
    {
        _pact.UponReceiving("get all blocked resources")
            .Given("there are blocked resources")
            .WithRequest(HttpMethod.Get, "/api/blocked-resources")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new
            {
                resources = new[]
                {
                    new { id = Match.Integer(1), status = Match.Regex("blocked", "available|blocked"), name = Match.Regex("LadyGaGa", ".*") },
                    new { id = Match.Integer(2), status = Match.Regex("blocked", "available|blocked"), name = Match.Regex("T-Love", ".*") },
                    new { id = Match.Integer(3), status = Match.Regex("blocked", "available|blocked"), name = Match.Regex("Snoop Dog", ".*") }
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
            var result = await client.GetBlocked();
            Assert.NotNull(result);
        });
    }
    
    [Fact]
    public async Task GetAllBlockedResourcesV2()
    {
        _pact.UponReceiving("get all blocked resources V2")
            .Given("there are blocked resources")
            .WithRequest(HttpMethod.Get, "/api/v2/blocked-resources")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new
            {
                resources = new[]
                {
                    new { id = Match.Integer(1), status = Match.Regex("blocked", "available|blocked") },
                    new { id = Match.Integer(2), status = Match.Regex("blocked", "available|blocked") },
                    new { id = Match.Integer(3), status = Match.Regex("blocked", "available|blocked") }
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
            var result = await client.GetBlockedV2();
            Assert.NotNull(result);
        });
    }

    [Fact]
    public async Task GetTemporaryBlocked()
    {
        _pact.UponReceiving("get temporary blocked resources")
            .Given("there are temporary blocked resources")
            .WithRequest(HttpMethod.Get, "/api/temporary-blocked")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new
            {
                resources = new[]
                {
                    new { id = Match.Integer(1), until = Match.Type(DateTime.Today) },
                    new { id = Match.Integer(2), until = Match.Type(DateTime.Today) },
                    new { id = Match.Integer(3), until = Match.Type(DateTime.Today) },
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
            var result = await client.GetTemporaryBlocked();
            Assert.NotNull(result);
        });
    }
}
