namespace BambooCard.WebAPI.Extensions;

public static class CorrelationIdExtensions
{
    private const string Header = "X-Correlation-ID";

    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.Use(async (ctx, next) =>
        {
            var id = ctx.Request.Headers[Header].FirstOrDefault()
                     ?? Guid.NewGuid().ToString();

            ctx.Request.Headers[Header] = id;
            ctx.Response.OnStarting(() =>
            {
                ctx.Response.Headers[Header] = id;
                return Task.CompletedTask;
            });

            ctx.TraceIdentifier = id;

            await next();
        });
    }
}