using ŠišAppApi.Models.Authentication;
using ŠišAppApi.Models.DTOs.Auth;

namespace ŠišAppApi.Services
{
    public interface IAuthenticationService
    {
        Task<TokenResponse?> Login(LoginRequest request);
        Task<TokenResponse> Register(RegisterDto request);
        Task<TokenResponse?> RefreshToken(string refreshToken);
        Task<bool> RevokeToken(string? refreshToken, int userId, string? accessTokenJti);
        Task RequestPasswordReset(PasswordResetRequestDto request);
        Task ResetPassword(PasswordResetConfirmDto request);
        Task ChangePassword(int userId, ChangePasswordDto request);
    }
} 