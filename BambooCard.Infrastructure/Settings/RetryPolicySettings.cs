namespace BambooCard.Infrastructure.Settings;

public class RetryPolicySettings
{
    public int RetryMaxAttempts { get; set; }
    public int RetryDelaySeconds { get; set; }
    public int AllowedFailuresBeforeBreak { get; set; }
    public int BreakDuration { get; set; }


    public int PermitLimit { get; set; }
    public int WindowSeconds { get; set; }
    public int QueueLimit { get; set; }
}
