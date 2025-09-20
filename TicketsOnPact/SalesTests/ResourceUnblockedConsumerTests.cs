using System.Text.Json;
using PactNet;
using PactNet.Output.Xunit;
using Sales;
using Sales.Events;
using Xunit.Abstractions;
using Match = PactNet.Matchers.Match;

namespace SalesTests;

public class ResourceUnblockedConsumerTests
{
    private readonly ResourceUnblockedEventHandler _eventHandler;

    private readonly IMessagePactBuilderV4 _pact;

    public ResourceUnblockedConsumerTests(ITestOutputHelper output)
    {
        _eventHandler = new ResourceUnblockedEventHandler();

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
    public async Task OnMessageAsync_ResourceUnlocked_HandlesMessage()
    {
        await _pact
            .ExpectsToReceive("an event indicating that an resource has been unblocked")
            .WithMetadata("eventType", "ResourceUnblocked")
            .WithJsonContent(new
            {
                Id = Match.Integer(1)
            })
            .VerifyAsync<ResourceUnblocked>(async message =>
            {
                Assert.True(await _eventHandler.Handle(message));
            });
    }
}