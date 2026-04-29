using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.DTOs.Auth;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Services;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfileController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IImageService _imageService;
    private readonly ICurrentUserService _currentUser;
    private readonly IAuthenticationService _authenticationService;

    public ProfileController(
        IUserService userService,
        IImageService imageService,
        ICurrentUserService currentUser,
        IAuthenticationService authenticationService)
    {
        _userService = userService;
        _imageService = imageService;
        _currentUser = currentUser;
        _authenticationService = authenticationService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
            return Unauthorized();

        var user = await _userService.GetById(userId);
        return Ok(user);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMyProfile(UserUpdateRequest request)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
            return Unauthorized();

        
        request.IsActive = null;

        var updated = await _userService.Update(userId, request);
        return Ok(updated);
    }

    [HttpPost("me/change-password")]
    public async Task<IActionResult> ChangeMyPassword(ChangePasswordDto request)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
            return Unauthorized();

        await _authenticationService.ChangePassword(userId, request);
        return Ok(new { message = "Lozinka je uspješno promijenjena." });
    }

    [HttpPost("me/upload-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<Image>> UploadMyProfileImage(IFormFile file)
    {
        var userId = GetCurrentUserId();
        if (userId <= 0)
            return Unauthorized();

        var user = await _userService.GetById(userId);
        if (user == null)
            return NotFound();

        if (!string.IsNullOrEmpty(user.ImageId))
        {
            await _imageService.DeleteAsync(user.ImageId);
        }

        var image = await _imageService.UploadImageAsync(file, "profile", userId, "User");
        await _userService.UpdateProfileImageAsync(userId, image.Id);

        return Ok(image);
    }

    private int GetCurrentUserId()
    {
        return _currentUser.UserId ?? 0;
    }
}
