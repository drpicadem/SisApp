using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UsersController : BaseCRUDController<UserDto, UserSearchObject, UserInsertRequest, UserUpdateRequest>
{
    private readonly IImageService _imageService;
    private readonly IUserService _userService;

    public UsersController(IUserService userService, IImageService imageService) : base(userService)
    {
        _userService = userService;
        _imageService = imageService;
    }

    [HttpDelete("{id}")]
    public override async Task<ActionResult<UserDto>> Delete(int id)
    {
        var currentUserId = GetUserId();
        if (currentUserId == id)
            throw new UserException("Ne možete obrisati vlastiti račun");

        return await base.Delete(id);
    }

    [HttpPut("{id}/restore")]
    public async Task<ActionResult<UserDto>> Restore(int id)
    {
        var result = await _userService.RestoreUser(id);
        return Ok(result);
    }

    [HttpPost("{id}/upload-profile-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<Image>> UploadProfileImage(int id, IFormFile file)
    {
        var user = await _userService.GetById(id);
        if (user == null) return NotFound();

        if (!string.IsNullOrEmpty(user.ImageId))
        {
            await _imageService.DeleteAsync(user.ImageId);
        }

        var image = await _imageService.UploadImageAsync(file, "profile", id, "User");
        await _userService.UpdateProfileImageAsync(id, image.Id);

        return Ok(image);
    }
}