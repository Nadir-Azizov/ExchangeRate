using BambooCard.Business.Models.User;

namespace BambooCard.Business.Abstractions;

public interface IAuthManager
{
    Task<bool> CreateUserAsync(RegisterDto model);
    Task<AuthResponseDto> LoginAsync(LoginDto model);
    Task<AuthResponseDto> RefreshTokenAsync(RefreshDto model);
    Task<UserDetailsDto> GetCurrentUserAsync(string userId);
    Task MakeUserAdminAsync(string email);
}
