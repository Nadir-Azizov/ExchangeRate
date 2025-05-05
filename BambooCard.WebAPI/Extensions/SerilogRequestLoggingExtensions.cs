using Serilog;

namespace BambooCard.WebAPI.Extensions;

public static class SerilogRequestLoggingExtensions
{
    public static IApplicationBuilder UseCustomSerilogRequestLogging(this IApplicationBuilder app)
    {
        return app.UseSerilogRequestLogging(opts =>
        {
            opts.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath}{QueryString} " +
                "responded {StatusCode} in {Elapsed:0.0000} ms " +
                "[CorrelationId: {CorrelationId}] " +
                "[ClientIp: {ClientIp}] " +
                "[ClientId: {ClientId}]";

            opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
            {
                diagCtx.Set("CorrelationId", httpCtx.TraceIdentifier);

                diagCtx.Set("ClientIp", httpCtx.Connection.RemoteIpAddress?.ToString());

                diagCtx.Set("QueryString", httpCtx.Request.QueryString.Value ?? string.Empty);

                var clientId = httpCtx.User?.FindFirst("client_id")?.Value
                               ?? httpCtx.User?.FindFirst(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub)?.Value
                               ?? "anonymous";

                diagCtx.Set("ClientId", clientId);
            };
        });
    }
}