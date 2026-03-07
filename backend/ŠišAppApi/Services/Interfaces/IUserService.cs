using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;

namespace ŠišAppApi.Services.Interfaces
{
    public interface IUserService : ICRUDService<UserDto, UserSearchObject, UserInsertRequest, UserUpdateRequest>
    {
        Task<UserDto> UpdateProfileImageAsync(int userId, string imageId);
        Task<UserDto> RestoreUser(int id);
    }
}
