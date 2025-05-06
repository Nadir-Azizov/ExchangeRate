using BambooCard.Infrastructure.Exceptions;
using BambooCard.Infrastructure.Settings;
using BambooCard.Tests.Helper;
using BambooCard.WebService.Implementions;
using BambooCard.WebService.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Polly;
using System.Net;

namespace BambooCard.Tests.WebService.Implementions;

public class FrankfurterProviderTests
{
    private static readonly string ValidJson = """
    {
        "base": "USD",
        "date": "2024-12-31",
        "rates": { "EUR": 0.89 }
    }
    """;

    [Fact]
    public async Task FetchLatestAsync_ShouldLogInfo_WhenSuccess()
    {
        var logger = Substitute.For<ILogger<FrankfurterProvider>>();

        var provider = CreateProvider(
            new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(ValidJson)
            }),
            logger);

        var result = await provider.FetchLatestAsync();

        Assert.Equal("USD", result.Base);
        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("retrieved rates")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }

    [Fact]
    public async Task FetchLatestAsync_ShouldLogError_WhenDeserializationFails()
    {
        var logger = Substitute.For<ILogger<FrankfurterProvider>>();

        var provider = CreateProvider(
            new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{ invalid json")
            }),
            logger);

        await Assert.ThrowsAsync<InternalServerException>(() => provider.FetchLatestAsync());

        logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("deserializing")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }

    [Fact]
    public async Task FetchLatestAsync_ShouldLogError_WhenStatusIsBad()
    {
        var logger = Substitute.For<ILogger<FrankfurterProvider>>();

        var provider = CreateProvider(
            new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway)
            {
                Content = new StringContent("Upstream error")
            }),
            logger);

        await Assert.ThrowsAsync<InternalServerException>(() => provider.FetchLatestAsync());

        logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Frankfurter API returned HTTP")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }

    [Fact]
    public async Task CircuitBreaker_ShouldOpen_HalfOpen_Reset()
    {
        // Arrange
        var logger = Substitute.For<ILogger<FrankfurterProvider>>();
        int callCount = 0;

        var failingHandler = new FakeHttpMessageHandler(_ =>
        {
            callCount++;
            throw new HttpRequestException("Boom");
        });

        var breakerPolicy = Policy<HttpResponseMessage>
            .Handle<HttpRequestException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 2,
                durationOfBreak: TimeSpan.FromMilliseconds(300),
                onBreak: (outcome, _) =>
                    logger.LogWarning("Breaker opened due to: {Error}", outcome.Exception?.Message),
                onReset: () =>
                    logger.LogInformation("Breaker reset"),
                onHalfOpen: () =>
                    logger.LogInformation("Breaker half-open"));

        var provider = CreateProvider(failingHandler, logger, breakerPolicy);

        await Assert.ThrowsAsync<InternalServerException>(() => provider.FetchLatestAsync());
        await Assert.ThrowsAsync<InternalServerException>(() => provider.FetchLatestAsync());

        var circuitEx = await Assert.ThrowsAsync<InternalServerException>(() => provider.FetchLatestAsync());

        Assert.Contains("Failed to GET Frankfurter rates", circuitEx.Message);
        Assert.Contains("open", circuitEx.Message, StringComparison.OrdinalIgnoreCase);

        await Task.Delay(350);

        var successHandler = new FakeHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(ValidJson)
        });

        var successProvider = CreateProvider(successHandler, logger, breakerPolicy);
        var result = await successProvider.FetchLatestAsync();

        Assert.Equal("USD", result.Base);

        logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Breaker opened")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());

        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Breaker half-open")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());

        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Breaker reset")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }

    private static FrankfurterProvider CreateProvider(
        HttpMessageHandler handler,
        ILogger<FrankfurterProvider> logger = null,
        IAsyncPolicy<HttpResponseMessage> policy = null)
    {
        var client = new HttpClient(handler);

        var settings = Options.Create(new ExchangeProviderSettings
        {
            FrankfurterLatestRateUrl = "https://api.frankfurter.app/latest"
        });

        var retrySettings = Options.Create(new RetryPolicySettings
        {
            RetryMaxAttempts = 3
        });

        logger ??= Substitute.For<ILogger<FrankfurterProvider>>();
        policy ??= Policy.NoOpAsync<HttpResponseMessage>();

        return new FrankfurterProvider(client, settings, retrySettings, logger, policy);
    }
}