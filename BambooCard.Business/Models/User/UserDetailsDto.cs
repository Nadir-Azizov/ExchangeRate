namespace BambooCard.Business.Models.User;

public record UserDetailsDto
{
    /// <summary>
    /// The user’s unique identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// The user’s email address.
    /// </summary>
    public string Email { get; set; }

    /// <summary>
    /// The user’s username (login name).
    /// </summary>
    public string UserName { get; set; }

    /// <summary>
    /// All the roles the user belongs to.
    /// </summary>
    public IEnumerable<string> Roles { get; set; } = [];
}
