using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using PactNet;
using PactNet.Output.Xunit;
using Sales.Services;
using Xunit.Abstractions;
using Match = PactNet.Matchers.Match;


namespace SalesTests;

public class AvailabilityConsumerFixture
{
    private readonly IPactBuilderV4 _pact;
    public IPactBuilderV4 PactBuilder { get; }

    public AvailabilityConsumerFixture(ITestOutputHelper output)
    {
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
        PactBuilder = _pact;
    }
    
    public void GivenResourceExists()
    {
        var expected = new ResourceDto(1, "LadyGaGa", "available");
        _pact.UponReceiving("a request to get resource by id")
            .Given("resource with ID {id} exists", new Dictionary<string, string> { ["id"] = "1" })
            .WithRequest(HttpMethod.Get, "/api/resources/1")
            .WillRespond()
            .WithStatus(HttpStatusCode.OK)
            .WithJsonBody(new
            {
                Id = Match.Integer(expected.Id), Name = Match.Regex(expected.Name, ".*"),
                Status = Match.Regex(expected.Status, "available|blocked")
            });

    }
}