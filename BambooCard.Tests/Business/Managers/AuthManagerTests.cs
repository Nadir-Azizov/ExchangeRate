using BambooCard.Business.Managers;
using BambooCard.Business.Models.User;
using BambooCard.Domain.DbContext;
using BambooCard.Domain.Entities.User;
using BambooCard.Domain.Settings;
using BambooCard.Infrastructure.Enums;
using BambooCard.Infrastructure.Exceptions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BambooCard.Tests.Business.Managers;

public class AuthManagerTests
{
    protected readonly UserManager<AppUser> userManager;
    protected readonly RoleManager<AppRole> roleManager;
    protected readonly IRoleStore<AppRole> roleStore;
    protected readonly IOptions<JwtSettings> jwtSettings;
    protected readonly BambooCardDbContext db;
    protected readonly AuthManager authManager;

    public AuthManagerTests()
    {
        var options = new DbContextOptionsBuilder<BambooCardDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        db = new BambooCardDbContext(options);

        var roleStore = new RoleStore<AppRole, BambooCardDbContext, int>(db);
        var roleValidators = new List<IRoleValidator<AppRole>> { new RoleValidator<AppRole>() };
        var lookupNorm = new UpperInvariantLookupNormalizer();
        var errorDesc = new IdentityErrorDescriber();
        var roleLogger = Substitute.For<ILogger<RoleManager<AppRole>>>();

        roleManager = new RoleManager<AppRole>(
            roleStore,
            roleValidators,
            lookupNorm,
            errorDesc,
            roleLogger
        );

        var userStore = Substitute.For<
            IUserStore<AppUser>,
            IUserAuthenticationTokenStore<AppUser>,
            IUserRoleStore<AppUser>>();
        userManager = Substitute.ForPartsOf<UserManager<AppUser>>(
            (IUserStore<AppUser>)userStore,
            null, null, null, null, null, null, null, null
        );

        jwtSettings = Options.Create(new JwtSettings
        {
            SecurityKey = "this_is_a_very_secure_key_32_bytes!",
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            SessionLifeTime = 30
        });

        authManager = new AuthManager(userManager, roleManager, jwtSettings, db);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnTokens_WhenCredentialsAreValid()
    {
        // Arrange
        var user = new AppUser { Id = 1, Email = "test@example.com" };
        var loginDto = new LoginDto { Email = "test@example.com", Password = "Pass123" };

        userManager.FindByEmailAsync(loginDto.Email).Returns(Task.FromResult(user));
        userManager.CheckPasswordAsync(user, loginDto.Password).Returns(Task.FromResult(true));
        userManager.GetRolesAsync(user).Returns(Task.FromResult<IList<string>>(new List<string> { "User" }));
        userManager.GenerateUserTokenAsync(user, Arg.Any<string>(), Arg.Any<string>()).Returns("refresh-token");

        // Act
        var result = await authManager.LoginAsync(loginDto);

        // Assert
        Assert.NotNull(result.AccessToken);
        Assert.Equal("refresh-token", result.RefreshToken);
        Assert.True(result.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenEmailIsInvalid()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "notfound@example.com", Password = "Pass123" };
        userManager.FindByEmailAsync(loginDto.Email).Returns(Task.FromResult<AppUser>(null));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedException>(() => authManager.LoginAsync(loginDto));
    }

    [Fact]
    public async Task LoginAsync_ShouldThrowUnauthorized_WhenPasswordIsInvalid()
    {
        // Arrange
        var user = new AppUser { Email = "user@example.com" };
        var loginDto = new LoginDto { Email = "user@example.com", Password = "wrong" };

        userManager.FindByEmailAsync(loginDto.Email).Returns(Task.FromResult(user));
        userManager.CheckPasswordAsync(user, loginDto.Password).Returns(Task.FromResult(false));

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => authManager.LoginAsync(loginDto));
    }

    [Fact]
    public async Task CreateUserAsync_ShouldSucceed_WhenValid()
    {
        // Arrange
        var registerDto = new RegisterDto { Email = "newuser@test.com", Password = "123456" };
        userManager.CreateAsync(Arg.Any<AppUser>(), registerDto.Password).Returns(IdentityResult.Success);

        userManager.AddToRoleAsync(Arg.Any<AppUser>(), "User").Returns(IdentityResult.Success);

        // Act
        var result = await authManager.CreateUserAsync(registerDto);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task RefreshTokenAsync_ShouldReturnNewTokens_WhenValid()
    {
        // Arrange
        var user = new AppUser { Id = 1, Email = "refresh@example.com" };
        var token = new IdentityUserToken<int>
        {
            UserId = user.Id,
            LoginProvider = "RefreshTokenProvider",
            Name = "RefreshToken",
            Value = "valid-token"
        };
        await db.Set<IdentityUserToken<int>>().AddAsync(token);
        await db.SaveChangesAsync();

        userManager.FindByIdAsync(user.Id.ToString()).Returns(user);
        userManager.VerifyUserTokenAsync(user, "RefreshTokenProvider", "RefreshToken", token.Value).Returns(true);
        userManager.GenerateUserTokenAsync(user, Arg.Any<string>(), Arg.Any<string>()).Returns("new-refresh");
        userManager.GetRolesAsync(user).Returns(Task.FromResult<IList<string>>(new List<string> { "User" }));

        // Act
        var result = await authManager.RefreshTokenAsync(new RefreshDto { RefreshToken = "valid-token" });

        // Assert
        Assert.NotNull(result.AccessToken);
        Assert.Equal("new-refresh", result.RefreshToken);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnUser_WhenFound()
    {
        // Arrange
        var user = new AppUser { Id = 1, Email = "user@x.com" };
        userManager.FindByIdAsync("1").Returns(user);
        userManager.GetRolesAsync(user).Returns(Task.FromResult<IList<string>>(new List<string> { "User" }));

        // Act
        var result = await authManager.GetCurrentUserAsync("1");

        // Assert
        Assert.Equal("user@x.com", result.Email);
        Assert.Contains("User", result.Roles);
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldThrow_WhenUserNotFound()
    {
        userManager.FindByIdAsync("123").Returns(Task.FromResult<AppUser>(null));

        await Assert.ThrowsAsync<BadRequestException>(() => authManager.GetCurrentUserAsync("123"));
    }

    [Fact]
    public async Task MakeUserAdminAsync_ShouldThrow_WhenUserNotFound2()
    {
        // Arrange
        userManager.FindByEmailAsync("missing@site.com").Returns((AppUser)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => authManager.MakeUserAdminAsync("missing@site.com")
        );
    }

    [Fact]
    public async Task MakeUserAdminAsync_ShouldCreateRole_WhenRoleDoesNotExist()
    {
        // Arrange
        var user = new AppUser { Email = "admin@site.com" };
        userManager.FindByEmailAsync(user.Email).Returns(user);

        Assert.False(await roleManager.RoleExistsAsync(EUserRole.Admin.ToString()));

        await roleManager.CreateAsync(new AppRole { Name = EUserRole.Admin.ToString() });

        userManager.AddToRoleAsync(user, EUserRole.Admin.ToString())
                   .Returns(Task.FromResult(IdentityResult.Success));

        // Act
        await authManager.MakeUserAdminAsync(user.Email);

        // Assert
        Assert.True(await roleManager.RoleExistsAsync(EUserRole.Admin.ToString()));
        await userManager.Received().AddToRoleAsync(user, EUserRole.Admin.ToString());
    }

    [Fact]
    public async Task MakeUserAdminAsync_ShouldAddRole_WhenRoleExists()
    {
        // Arrange
        var user = new AppUser { Email = "admin@site.com" };
        userManager.FindByEmailAsync(user.Email).Returns(user);
        await roleManager.CreateAsync(new AppRole { Name = EUserRole.Admin.ToString() });

        userManager.AddToRoleAsync(user, EUserRole.Admin.ToString()).Returns(IdentityResult.Success);

        // Act
        await authManager.MakeUserAdminAsync(user.Email);

        // Assert
        await userManager.Received().AddToRoleAsync(user, EUserRole.Admin.ToString());
    }

    [Fact]
    public async Task MakeUserAdminAsync_ShouldThrow_WhenUserNotFound()
    {
        // Arrange
        userManager.FindByEmailAsync("missing@site.com").Returns((AppUser)null);

        // Act & Assert
        await Assert.ThrowsAsync<NotFoundException>(
            () => authManager.MakeUserAdminAsync("missing@site.com")
        );
    }

    [Fact]
    public async Task CreateUserAsync_ShouldThrow_WhenCreateFails()
    {
        var dto = new RegisterDto { /*…*/ };
        userManager.CreateAsync(Arg.Any<AppUser>(), dto.Password)
                   .Returns(IdentityResult.Failed(new IdentityError { Description = "oops" }));

        await Assert.ThrowsAsync<BadRequestException>(() => authManager.CreateUserAsync(dto));
    }
}
