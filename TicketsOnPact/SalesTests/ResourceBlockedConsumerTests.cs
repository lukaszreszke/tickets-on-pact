using System.Text.Json;
using PactNet;
using PactNet.Output.Xunit;
using Sales;
using Sales.Events;
using Xunit.Abstractions;
using Match = PactNet.Matchers.Match;

namespace SalesTests;

public class ResourceBlockedConsumerTests
{
    private readonly ResourceBlockedEventHandler _eventHandler;

    private readonly IMessagePactBuilderV4 _pact;

    public ResourceBlockedConsumerTests(ITestOutputHelper output)
    {
        _eventHandler = new ResourceBlockedEventHandler();

        var config = new PactConfig
        {
            PactDir = "../../../pacts/",
            Outputters = new[]
            {
                new XunitOutput(output)
            },
            DefaultJsonSettings = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                PropertyNameCaseInsensitive = true
            }
        };

        _pact = Pact.V4("Sales", "AvailabilityApi", config).WithMessageInteractions();
    }

    [Fact]
    public async Task OnMessageAsync_ResourceBlocked_HandlesMessage()
    {
        await _pact
            .ExpectsToReceive("an event indicating that an resource has been blocked")
            .WithMetadata("eventType", "ResourceBlocked")
            .WithJsonContent(new
            {
                Id = Match.Integer(1)
            })
            .VerifyAsync<ResourceBlocked>(async message =>
            {
                Assert.True(await _eventHandler.Handle(message));
            });
    }
}