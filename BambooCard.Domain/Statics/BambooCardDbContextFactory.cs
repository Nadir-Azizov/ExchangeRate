using BambooCard.Domain.DbContext;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace BambooCard.Domain.Statics;

public class BambooCardDbContextFactory : IDesignTimeDbContextFactory<BambooCardDbContext>
{
    public BambooCardDbContext CreateDbContext(string[] args)
    {
        var basePath = Path.Combine(Directory.GetCurrentDirectory(), "../BambooCard.WebAPI");

        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        var connectionString = configuration.GetConnectionString("DefaultConnection");

        var optionsBuilder = new DbContextOptionsBuilder<BambooCardDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new BambooCardDbContext(optionsBuilder.Options);
    }
}
