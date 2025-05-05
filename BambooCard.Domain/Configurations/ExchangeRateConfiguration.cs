using BambooCard.Domain.Entities.Main;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BambooCard.Domain.Configurations;

internal class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.BaseCurrency);
        builder.Property(x => x.Amount);
        builder.Property(x => x.Date);
        builder.Property(x => x.Provider);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()").ValueGeneratedOnAdd();

        builder.HasIndex(x => new { x.BaseCurrency, x.Date }).IsUnique();
    }
}