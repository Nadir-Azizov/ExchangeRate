namespace BambooCard.Business.Models.User;

public record AuthResponseDto
{
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
}
