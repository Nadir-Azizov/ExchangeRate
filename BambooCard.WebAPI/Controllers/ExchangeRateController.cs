using Asp.Versioning;
using BambooCard.Business.Abstractions;
using BambooCard.Business.Models.Main;
using BambooCard.Domain.Enums;
using BambooCard.Infrastructure.Enums;
using BambooCard.Infrastructure.Results;
using BambooCard.WebAPI.Controllers.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BambooCard.WebAPI.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public class ExchangeRateController(IExchangeManager exchangeManager) : CustomController
{
    [HttpGet("service")]
    [Authorize(Roles = nameof(EUserRole.Admin))]
    public async Task<ActionResult<ResponseResult<ExchangeRateDto>>> GetServiceRate([FromQuery] EProvider provider)
    {
        return Ok(await exchangeManager.GetServiceRateAsync(provider));
    }

    [HttpGet("current")]
    public async Task<ActionResult<ResponseResult<ExchangeRateDto>>> GetCurrentRate()
    {
        return Ok(await exchangeManager.GetCurrentRateAsync());
    }

    [HttpPost("search")]
    public async Task<ActionResult<ResponseResult<PaginationResult<ExchangeRateDto>>>> SearchRates(
        [FromBody] ExchangeSearchModel model)
    {
        return Ok(await exchangeManager.SearchRatesAsync(model));
    }

    [HttpGet("convert")]
    public async Task<ActionResult<ResponseResult<Dictionary<ECurrency, decimal>>>> ConvertToAll(
        [FromQuery] ECurrency fromCurrency, [FromQuery] decimal amount)
    {
        return Ok(await exchangeManager.ConvertToAllAsync(fromCurrency, amount));
    }

    [HttpPost("cache/refresh")]
    public async Task<IActionResult> RefreshCache()
    {
        await exchangeManager.RefreshCacheFromDbAsync();
        return NoContent();
    }

    [HttpPost("import")]
    [Authorize(Roles = nameof(EUserRole.Admin))]
    public async Task<ActionResult<ResponseResult<ExchangeRateDto>>> ImportLatest(EProvider provider)
    {
        return Ok(await exchangeManager.ImportLatestAsync(provider));
    }
}
