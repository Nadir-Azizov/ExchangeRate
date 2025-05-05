using Microsoft.AspNetCore.Identity;

namespace BambooCard.Domain.Entities.User;

public class AppUser : IdentityUser<int>
{
    public string Name { get; set; }
    public string Surname { get; set; }
    public DateTimeOffset? Birthday { get; set; }
}