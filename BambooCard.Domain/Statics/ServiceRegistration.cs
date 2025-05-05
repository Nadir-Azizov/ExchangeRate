using BambooCard.Domain.Abstractions;
using BambooCard.Domain.Abstractions.Base;
using BambooCard.Domain.DbContext;
using BambooCard.Domain.Entities.User;
using BambooCard.Domain.Repositories;
using BambooCard.Domain.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BambooCard.Domain.Statics;

public static class ServiceRegistration
{
    public static IServiceCollection AddDomainDependencies(this IServiceCollection services, ConfigurationManager configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(nameof(JwtSettings)));


        services.AddDbContext<BambooCardDbContext>(opts =>
            opts.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services
            .AddScoped<IExchangeRateRepository, ExchangeRateRepository>()
            .AddScoped<IRateRepository, RateRepository>()
            .AddScoped(typeof(IRepository<>), typeof(Repository<>));

        services
            .AddIdentity<AppUser, AppRole>(options =>
            {
                options.User.RequireUniqueEmail = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<BambooCardDbContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<DataProtectorTokenProvider<AppUser>>("RefreshTokenProvider");


        var jwtSettings = configuration.GetSection(nameof(JwtSettings)).Get<JwtSettings>();

        var key = Encoding.UTF8.GetBytes(jwtSettings.SecurityKey);

        services.Configure<DataProtectionTokenProviderOptions>(opts =>
        {
            opts.TokenLifespan = TimeSpan.FromDays(jwtSettings.RefreshTokenLifeTime);
        });

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        return services;
    }
}