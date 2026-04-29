using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.DTOs.Auth;

public class PasswordResetConfirmDto
{
    [Required(ErrorMessage = "Email je obavezan")]
    [EmailAddress(ErrorMessage = "Neispravan format email adrese")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Reset token je obavezan")]
    [StringLength(200, MinimumLength = 8, ErrorMessage = "Reset token nije validan")]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lozinka je obavezna")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Lozinka mora imati minimalno 4 znaka")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Potvrda lozinke je obavezna")]
    [Compare("NewPassword", ErrorMessage = "Lozinke se ne podudaraju")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
