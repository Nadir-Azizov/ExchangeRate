using BambooCard.Infrastructure.Helper;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Polly.CircuitBreaker;
using System.Net;

namespace BambooCard.Tests.Infrastructure.Helper;

public class RetryHelperTests
{
    private readonly ILogger _logger = Substitute.For<ILogger>();

    [Fact]
    public async Task ShouldRetry_OnTransientFailures()
    {
        // Arrange
        int attempts = 0;
        var policy = RetryHelper.CreateResiliencePolicy(
            retryCount: 3,
            baseDelaySeconds: 0, // No actual delay in test
            allowedFailuresBeforeBreak: 5,
            breakDuration: TimeSpan.FromSeconds(10),
            logger: _logger);

        Func<Task<HttpResponseMessage>> action = () =>
        {
            attempts++;
            return Task.FromResult(new HttpResponseMessage(
                attempts < 3 ? HttpStatusCode.ServiceUnavailable : HttpStatusCode.OK));
        };

        // Act
        var result = await policy.ExecuteAsync(action);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
        Assert.Equal(3, attempts);

        _logger.Received(2).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Retry")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }

    [Fact]
    public async Task ShouldOpenCircuitBreaker_AfterConsecutiveFailures()
    {
        // Arrange
        var policy = RetryHelper.CreateResiliencePolicy(
            retryCount: 1,
            baseDelaySeconds: 0,
            allowedFailuresBeforeBreak: 2,
            breakDuration: TimeSpan.FromSeconds(10),
            logger: _logger);

        Func<Task<HttpResponseMessage>> failingAction = () =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError)); // 500 = transient error

        // Act - First two attempts to trip breaker
        await policy.ExecuteAsync(failingAction);
        await policy.ExecuteAsync(failingAction);

        // Act + Assert - Third attempt should trigger circuit breaker
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(() =>
            policy.ExecuteAsync(failingAction));

        _logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Circuit opened")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }

    [Fact]
    public async Task ShouldResetCircuitBreaker_AfterTimeout()
    {
        // Arrange
        var breakDuration = TimeSpan.FromMilliseconds(500);
        var policy = RetryHelper.CreateResiliencePolicy(
            retryCount: 1,
            baseDelaySeconds: 0,
            allowedFailuresBeforeBreak: 1,
            breakDuration: breakDuration,
            logger: _logger);

        Func<Task<HttpResponseMessage>> fail = () =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));

        Func<Task<HttpResponseMessage>> success = () =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        // Act - Trip breaker
        await policy.ExecuteAsync(fail);

        // Act - Should throw during open state
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(() => policy.ExecuteAsync(fail));

        // Wait for breaker to reset
        await Task.Delay(breakDuration + TimeSpan.FromMilliseconds(100));

        // Act - Should pass after reset
        var result = await policy.ExecuteAsync(success);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Circuit closed")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());

        _logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Circuit half-open")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }


    [Fact]
    public async Task ShouldLog_AllCircuitBreakerEvents()
    {
        // Arrange
        var breakDuration = TimeSpan.FromMilliseconds(300);
        var retryCount = 2;

        var logger = Substitute.For<ILogger>();
        var policy = RetryHelper.CreateResiliencePolicy(
            retryCount: retryCount,
            baseDelaySeconds: 0,
            allowedFailuresBeforeBreak: 2,
            breakDuration: breakDuration,
            logger: logger);

        int failCounter = 0;

        // Fail twice to trigger retries and open the circuit
        Func<Task<HttpResponseMessage>> fail = () =>
        {
            failCounter++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.InternalServerError));
        };

        await policy.ExecuteAsync(fail);
        await policy.ExecuteAsync(fail);

        // Circuit should now be open
        await Assert.ThrowsAsync<BrokenCircuitException<HttpResponseMessage>>(() => policy.ExecuteAsync(fail));

        // Wait for reset period
        await Task.Delay(breakDuration + TimeSpan.FromMilliseconds(100));

        // Half-open test probe (success)
        var result = await policy.ExecuteAsync(() =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)));

        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        // ✅ Assert log: retry warnings (2 failures × 1 retry each)
        logger.Received().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Retry")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());

        // ✅ Assert log: circuit breaker opened
        logger.Received().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Circuit opened")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());

        // ✅ Assert log: half-open and reset
        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Circuit half-open")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());

        logger.Received().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Circuit closed")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }

    [Fact]
    public async Task ShouldNotRetry_OnSuccess()
    {
        // Arrange
        var logger = Substitute.For<ILogger>();
        var policy = RetryHelper.CreateResiliencePolicy(
            retryCount: 3,
            baseDelaySeconds: 0,
            allowedFailuresBeforeBreak: 2,
            breakDuration: TimeSpan.FromSeconds(5),
            logger: logger);

        Func<Task<HttpResponseMessage>> successAction = () =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK));

        // Act
        var result = await policy.ExecuteAsync(successAction);

        // Assert
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);

        // ✅ Ensure no retry log is written
        logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());

        // ✅ Ensure no circuit breaker logs
        logger.DidNotReceive().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());

        logger.DidNotReceive().Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception, string>>());
    }
}