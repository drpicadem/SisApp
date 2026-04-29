using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.DTOs.Auth;

public class ChangePasswordDto
{
    [Required(ErrorMessage = "Trenutna lozinka je obavezna")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Trenutna lozinka mora imati između 4 i 100 znakova")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Nova lozinka je obavezna")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Nova lozinka mora imati između 4 i 100 znakova")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Potvrda nove lozinke je obavezna")]
    [Compare("NewPassword", ErrorMessage = "Potvrda lozinke mora biti ista kao nova lozinka")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
