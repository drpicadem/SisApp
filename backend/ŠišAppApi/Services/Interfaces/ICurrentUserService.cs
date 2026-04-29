namespace ŠišAppApi.Services.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string Role { get; }
    string Username { get; }
}
