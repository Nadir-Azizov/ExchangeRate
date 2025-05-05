namespace BambooCard.Infrastructure.Settings;

public class OpenTelemetrySettings
{
    public string ConnectionString { get; set; }
    public bool EnableSqlClientInstrumentation { get; set; }
}
