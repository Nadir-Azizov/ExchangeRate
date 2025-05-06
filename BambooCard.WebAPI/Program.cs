using BambooCard.Business.Statics;
using BambooCard.Domain.Statics;
using BambooCard.Infrastructure.Statics;
using BambooCard.WebAPI.Extensions;
using BambooCard.WebAPI.Middlewares;
using BambooCard.WebService.Statics;
using Serilog;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHealthChecks(builder.Configuration);

builder.Services.AddVersioningWithExplorer();

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
    {
        opts.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGenWithVersioning();

#region ========== Project Dependencies ==========
builder.Services.AddInfrastructureDependencies(builder.Configuration);
builder.Services.AddDomainDependencies(builder.Configuration);
builder.Services.AddBusinessDependencies(builder.Configuration);
builder.Services.AddWebServiceDependencies(builder.Configuration);
#endregion ========== Project Dependencies ==========


builder.Services.AddCustomOpenTelemetry(builder.Configuration);

#region ========== Logging ==========
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

#endregion ========== Logging ==========

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUIWithVersioning();
}


app.MapHealthCheckLive();
app.MapHealthCheckReady();

app.UseCorrelationId();

app.UseMiddleware<ExceptionHandlerMiddleware>();

app.UseCustomSerilogRequestLogging();

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

namespace BambooCard.WebAPI
{
    public partial class Program { }
}
