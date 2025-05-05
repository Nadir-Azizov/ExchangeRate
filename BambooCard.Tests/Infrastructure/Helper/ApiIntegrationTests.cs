using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;


namespace BambooCard.Tests.Infrastructure.Helper;

public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<BambooCard.WebAPI.Program>>
{
    private readonly HttpClient _client;

    public ApiIntegrationTests(WebApplicationFactory<BambooCard.WebAPI.Program> factory)
    {
        var webApiAssemblyName = typeof(BambooCard.WebAPI.Program).Assembly.GetName().Name!;

        _client = factory.WithWebHostBuilder(builder => { }).CreateClient();
    }

    [Fact]
    public async Task HealthEndpoint_Returns200Ok()
    {
        var resp = await _client.GetAsync("/api/ExchangeRate/current");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task RateLimit_ExceedLimit_Returns429()
    {
        int ok = 0, tooMany = 0;
        for (int i = 0; i < 20; i++)
        {
            var r = await _client.GetAsync("/api/ExchangeRate/current");
            if (r.StatusCode == HttpStatusCode.OK) ok++;
            if (r.StatusCode == HttpStatusCode.TooManyRequests) tooMany++;
        }

        Assert.Equal(10, ok);
        Assert.Equal(10, tooMany);
    }

    [Fact]
    public async Task UnknownEndpoint_Returns404()
    {
        var resp = await _client.GetAsync("/api/exchange/does-not-exist");
        Assert.Equal(HttpStatusCode.NotFound, resp.StatusCode);
    }
}
