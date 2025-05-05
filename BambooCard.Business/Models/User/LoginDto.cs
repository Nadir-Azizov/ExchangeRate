namespace BambooCard.Business.Models.User;

public record LoginDto
{
    public string Email { get; set; }
    public string Password { get; set; }
}
