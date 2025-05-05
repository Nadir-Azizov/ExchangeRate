namespace BambooCard.Domain.Settings;

public class JwtSettings
{
    public string SecurityKey { get; set; }
    public string Audience { get; set; }
    public string Issuer { get; set; }
    public double SessionLifeTime { get; set; }
    public double RefreshTokenLifeTime { get; set; }
}
