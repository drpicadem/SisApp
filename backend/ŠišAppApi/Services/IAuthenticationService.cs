using ŠišAppApi.Models.Authentication;

namespace ŠišAppApi.Services
{
    public interface IAuthenticationService
    {
        Task<TokenResponse?> Login(LoginRequest request);
        Task<TokenResponse?> RefreshToken(string refreshToken);
        Task<bool> RevokeToken(string refreshToken);
    }
} 