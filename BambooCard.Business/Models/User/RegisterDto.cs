namespace BambooCard.Business.Models.User;

public record RegisterDto
{
    public string Name { get; set; }
    public string Surname { get; set; }
    public string Email { get; set; }
    public DateTimeOffset? Birthday { get; set; }
    public string Password { get; set; }
}
