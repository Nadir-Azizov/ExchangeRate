using System.Text;
using System.Text.Json;

namespace BambooCard.WebAPI.Middlewares;

public class LoggingMiddleware(RequestDelegate next, ILogger<LoggingMiddleware> logger)
{
    private const string Divider = "────────────────────────────────────────────────────────────────";

    public async Task InvokeAsync(HttpContext context)
    {
        var requestLog = await BuildRequestLog(context);
        logger.LogInformation(
            "{Divider}\nREQUEST:\n{RequestLog}\n{Divider}",
            Divider, requestLog);

        var originalBody = context.Response.Body;
        await using var memStream = new MemoryStream();
        context.Response.Body = memStream;

        await next(context);

        memStream.Seek(0, SeekOrigin.Begin);
        var responseLog = await BuildResponseLog(context.Response, memStream);
        logger.LogInformation(
            "{Divider}\nRESPONSE:\n{ResponseLog}\n{Divider}",
            Divider, responseLog);

        memStream.Seek(0, SeekOrigin.Begin);
        await memStream.CopyToAsync(originalBody);
    }

    private async Task<string> BuildRequestLog(HttpContext ctx)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CorrelationId: {ctx.TraceIdentifier}");
        sb.AppendLine($"Timestamp: {DateTime.UtcNow:O}");
        sb.AppendLine($"Method:    {ctx.Request.Method}");
        sb.AppendLine($"Path:      {ctx.Request.Path + ctx.Request.QueryString}");
        sb.AppendLine($"ClientIp:  {ctx.Connection.RemoteIpAddress}");
        sb.AppendLine($"ClientId:  {ctx.User?.FindFirst("client_id")?.Value ?? "anonymous"}");
        sb.AppendLine();

        // Body
        if (ctx.Request.ContentLength > 0 &&
            ctx.Request.ContentType?.Contains("application/json") == true)
        {
            ctx.Request.EnableBuffering();
            ctx.Request.Body.Seek(0, SeekOrigin.Begin);

            using var reader = new StreamReader(ctx.Request.Body, leaveOpen: true);
            var raw = await reader.ReadToEndAsync();
            ctx.Request.Body.Seek(0, SeekOrigin.Begin);

            sb.AppendLine("Body:");
            sb.AppendLine(BeautifyJson(raw));
        }

        return sb.ToString();
    }

    private async Task<string> BuildResponseLog(HttpResponse res, MemoryStream mem)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"CorrelationId: {res.HttpContext.TraceIdentifier}");
        sb.AppendLine($"Timestamp:       {DateTime.UtcNow:O}");
        sb.AppendLine($"StatusCode:      {res.StatusCode}");
        sb.AppendLine($"ContentType:     {res.ContentType}");
        sb.AppendLine();

        if (res.ContentType?.Contains("application/json") == true)
        {
            mem.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(mem, leaveOpen: true);
            var raw = await reader.ReadToEndAsync();
            mem.Seek(0, SeekOrigin.Begin);

            sb.AppendLine("Body:");
            sb.AppendLine(BeautifyJson(raw));
        }

        return sb.ToString();
    }

    private static string BeautifyJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            return JsonSerializer.Serialize(
                doc.RootElement,
                new JsonSerializerOptions { WriteIndented = true }
            );
        }
        catch
        {
            return json;
        }
    }
}
