using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace BambooCard.WebAPI.HealthChecks;

public class SqlServerHealthCheck(IConfiguration config) : IHealthCheck
{
    private readonly string _cs = config.GetConnectionString("DefaultConnection");

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken ct)
    {
        try
        {
            using var conn = new SqlConnection(_cs);
            await conn.OpenAsync(ct);
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(exception: ex);
        }
    }
}
