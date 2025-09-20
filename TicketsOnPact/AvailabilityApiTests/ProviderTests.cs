using System.Text.Json;
using AvailabilityApi.Events;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using PactNet;
using PactNet.Infrastructure.Outputters;
using PactNet.Output.Xunit;
using PactNet.Verifier;
using Xunit.Abstractions;

namespace AvailabilityApiTests;

public class ProviderTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly PactVerifier _verifier;
    private readonly WebApplication _app;
    private readonly HttpClient _client;
    private readonly string _baseUri = "http://localhost:9876";


    private static readonly JsonSerializerSettings Options = new JsonSerializerSettings()
    {
        ContractResolver = new CamelCasePropertyNamesContractResolver(),
        NullValueHandling = NullValueHandling.Ignore,
        DateFormatHandling = DateFormatHandling.IsoDateFormat,
        DateTimeZoneHandling = DateTimeZoneHandling.Utc,
        ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
    };
    
    public ProviderTests(ITestOutputHelper output)
    {
        _output = output;

        _app = Program.BuildApp(["--environment=Testing"]);
        _app.Urls.Add(_baseUri);
        _app.UseMiddleware<ProviderStateMiddleware>(); 
        _app.Start();

        _client = new HttpClient { BaseAddress = new Uri(_baseUri) };

        _verifier = new PactVerifier("AvailabilityApi", new PactVerifierConfig
        {
            LogLevel = PactLogLevel.Error,
            Outputters = new List<IOutput>
            {
                new XunitOutput(output)
            }
        });
    }

    [Fact]
    public void Verify()
    {
        _verifier.WithHttpEndpoint(new Uri(_baseUri))
            .WithMessages(scenarios =>
            {
                scenarios.Add("an event indicating that an resource has been blocked", builder =>
                {
                    builder.WithMetadata(new
                    {
                        eventType = nameof(ResourceBlocked),
                        contentType = "application/json"
                    }).WithContent(() => new ResourceBlocked(1));
                });
                scenarios.Add("an event indicating that an resource has been unblocked", builder =>
                {
                    builder.WithMetadata(new
                    {
                        eventType = nameof(ResourceUnblocked),
                        contentType = "application/json"
                    }).WithContent(() => new ResourceUnblocked(1));
                });
            }, JsonSerializerOptions.Web)
            .WithPactBrokerSource(new Uri("http://localhost:9292"), options =>
            {
                options.PublishResults(true, version);
            })
            .WithProviderStateUrl(new Uri($"{_baseUri}/provider-states"))
            .Verify();
    }

    public void Dispose()
    {
        _verifier.Dispose();
        _client.Dispose();
        _app.DisposeAsync().GetAwaiter().GetResult();
    }
}