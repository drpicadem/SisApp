using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ŠišAppApi.Constants;
using ŠišAppApi.Filters;
using ŠišAppApi.Models;
using ŠišAppApi.Models.DTOs;
using ŠišAppApi.Models.Requests;
using ŠišAppApi.Models.SearchObjects;
using ŠišAppApi.Services.Interfaces;

namespace ŠišAppApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = AppRoles.Admin)]
public class UsersController : BaseCRUDController<UserDto, UserSearchObject, UserInsertRequest, UserUpdateRequest>
{
    private readonly IImageService _imageService;
    private readonly IUserService _userService;
    private readonly IAdminLogService _adminLogService;

    public UsersController(
        IUserService userService,
        IImageService imageService,
        IAdminLogService adminLogService,
        ICurrentUserService currentUser) : base(userService, currentUser)
    {
        _userService = userService;
        _imageService = imageService;
        _adminLogService = adminLogService;
    }

    [HttpPost]
    public override async Task<ActionResult<UserDto>> Insert([FromBody] UserInsertRequest request)
    {
        var result = await base.Insert(request);
        if (result.Value != null && _currentUser.UserId.HasValue)
        {
            await _adminLogService.LogAsync(
                _currentUser.UserId.Value,
                "Create User", "User", result.Value.Id,
                $"Kreiran korisnik: {result.Value.Username}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());
        }
        return result;
    }

    [HttpPut("{id}")]
    public override async Task<ActionResult<UserDto>> Update(int id, [FromBody] UserUpdateRequest request)
    {
        var result = await base.Update(id, request);
        if (_currentUser.UserId.HasValue)
        {
            await _adminLogService.LogAsync(
                _currentUser.UserId.Value,
                "Update User", "User", id,
                $"Ažuriran korisnik: {request.Username}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());
        }
        return result;
    }

    [HttpPost("{id}/set-password")]
    public async Task<IActionResult> SetPassword(int id, [FromBody] AdminSetPasswordRequest request)
    {
        await _userService.SetPasswordByAdmin(id, request.NewPassword, request.ConfirmPassword);
        if (_currentUser.UserId.HasValue)
        {
            await _adminLogService.LogAsync(
                _currentUser.UserId.Value,
                "Set User Password", "User", id,
                "Administrator promijenio lozinku korisnika",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());
        }
        return Ok(new { message = "Lozinka korisnika je uspješno promijenjena." });
    }

    [HttpDelete("{id}")]
    public override async Task<ActionResult<UserDto>> Delete(int id)
    {
        var currentUserId = GetUserId();
        if (currentUserId == id)
            throw new UserException("Ne možete obrisati vlastiti račun");

        var existingUser = await _userService.GetById(id);
        var result = await base.Delete(id);
        if (_currentUser.UserId.HasValue)
        {
            await _adminLogService.LogAsync(
                _currentUser.UserId.Value,
                "Delete User", "User", id,
                existingUser != null ? $"Obrisan korisnik: {existingUser.Username}" : "Obrisan korisnik",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());
        }
        return result;
    }

    [HttpPut("{id}/restore")]
    public async Task<ActionResult<UserDto>> Restore(int id)
    {
        var result = await _userService.RestoreUser(id);
        if (_currentUser.UserId.HasValue)
        {
            await _adminLogService.LogAsync(
                _currentUser.UserId.Value,
                "Restore User", "User", id,
                $"Vraćen korisnik: {result.Username}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());
        }
        return Ok(result);
    }

    [HttpPost("{id}/upload-profile-image")]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<ActionResult<Image>> UploadProfileImage(int id, IFormFile file)
    {
        var user = await _userService.GetById(id);
        if (user == null) return NotFound();

        if (!string.IsNullOrEmpty(user.ImageId))
            await _imageService.DeleteAsync(user.ImageId);

        var image = await _imageService.UploadImageAsync(file, "profile", id, "User");
        await _userService.UpdateProfileImageAsync(id, image.Id);

        if (_currentUser.UserId.HasValue)
        {
            await _adminLogService.LogAsync(
                _currentUser.UserId.Value,
                "Upload User Image", "User", id,
                $"Ažurirana profilna slika za korisnika: {user.Username}",
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString());
        }

        return Ok(image);
    }
}
