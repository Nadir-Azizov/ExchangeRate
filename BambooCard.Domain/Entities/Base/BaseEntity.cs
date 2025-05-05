namespace BambooCard.Domain.Entities.Base;

public abstract class BaseEntity
{
    public int Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
