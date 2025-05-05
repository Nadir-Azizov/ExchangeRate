using BambooCard.Infrastructure.Enums;
using BambooCard.Infrastructure.Exceptions;
using BambooCard.Infrastructure.Settings;
using BambooCard.WebService.Abstractions;
using BambooCard.WebService.Models;
using BambooCard.WebService.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using System.Net.Http.Json;
using System.Text.Json;

namespace BambooCard.WebService.Implementions;

public class FrankfurterProvider(
    HttpClient client,
    IOptions<ExchangeProviderSettings> exchangeProviderSettings,
    IOptions<RetryPolicySettings> retryPolicySettings,
    ILogger<FrankfurterProvider> logger,
    IAsyncPolicy<HttpResponseMessage> resiliencePolicy) : ICurrencyProvider
{
    public EProvider Provider => EProvider.Frankfurter;

    /// <summary>
    /// Fetches the latest exchange rates from Frankfurter, retrying on transient failures.
    /// </summary>
    public async Task<ExchangeRateDto> FetchLatestAsync(CancellationToken cancellationToken = default)
    {
        HttpResponseMessage response;
        try
        {
            response = await resiliencePolicy.ExecuteAsync(
                ct => client.GetAsync(exchangeProviderSettings.Value.FrankfurterLatestRateUrl, ct),
                cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "All {MaxAttempts} Frankfurter attempts failed.", retryPolicySettings.Value.RetryMaxAttempts);
            throw new InternalServerException($"Failed to GET Frankfurter rates after {retryPolicySettings.Value.RetryMaxAttempts} attempts: {ex.Message}");
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogError("Frankfurter API returned HTTP {StatusCode}: {Error}", (int)response.StatusCode, error);

            throw new InternalServerException($"Frankfurter API returned {(int)response.StatusCode}: {error}");
        }

        try
        {
            var dto = await response.Content.ReadFromJsonAsync<ExchangeRateDto>(
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken)
                    ?? throw new InternalServerException("Frankfurter returned an empty body.");

            logger.LogInformation("Successfully retrieved rates for base currency {BaseCurrency} at {Date}.", dto.Base, dto.Date);

            return dto;
        }
        catch (JsonException jex)
        {
            logger.LogError(jex, "Error deserializing Frankfurter JSON.");
            throw new InternalServerException($"Error parsing Frankfurter response: {jex.Message}");
        }
    }
}
