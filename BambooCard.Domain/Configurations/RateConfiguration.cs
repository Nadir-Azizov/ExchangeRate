using BambooCard.Domain.Entities.Main;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BambooCard.Domain.Configurations;

internal class RateConfiguration : IEntityTypeConfiguration<Rate>
{
    public void Configure(EntityTypeBuilder<Rate> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Currency);
        builder.Property(x => x.Value);
        builder.Property(x => x.ExchangeRateId);

        builder.Property(x => x.CreatedAt).HasDefaultValueSql("SYSDATETIMEOFFSET()").ValueGeneratedOnAdd();

        builder.HasOne(x => x.ExchangeRate)
            .WithMany(x => x.Rates)
            .HasForeignKey(x => x.ExchangeRateId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}