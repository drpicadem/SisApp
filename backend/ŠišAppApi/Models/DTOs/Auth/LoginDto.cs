using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.DTOs.Auth;

public class LoginDto
{
    [Required(ErrorMessage = "Email je obavezan")]
    [EmailAddress(ErrorMessage = "Neispravan format email adrese")]
    [StringLength(100, ErrorMessage = "Email ne smije biti duži od 100 znakova")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lozinka je obavezna")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Lozinka mora imati između 6 i 100 znakova")]
    public string Password { get; set; } = string.Empty;
} 