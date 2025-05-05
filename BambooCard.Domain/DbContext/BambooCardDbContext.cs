using BambooCard.Domain.Entities.Main;
using BambooCard.Domain.Entities.User;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace BambooCard.Domain.DbContext;

public class BambooCardDbContext : IdentityDbContext<AppUser, AppRole, int>
{
    public BambooCardDbContext(DbContextOptions<BambooCardDbContext> options) : base(options) { }

    public DbSet<ExchangeRate> ExchangeRates { get; set; }
    public DbSet<Rate> Rates { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
