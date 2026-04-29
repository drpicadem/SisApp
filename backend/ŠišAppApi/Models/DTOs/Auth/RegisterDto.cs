using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.DTOs.Auth;

public class RegisterDto
{
    [Required(ErrorMessage = "Korisničko ime je obavezno")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "Korisničko ime mora imati između 3 i 50 znakova")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ime je obavezno")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Ime mora imati između 2 i 50 znakova")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Prezime je obavezno")]
    [StringLength(50, MinimumLength = 2, ErrorMessage = "Prezime mora imati između 2 i 50 znakova")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email je obavezan")]
    [EmailAddress(ErrorMessage = "Neispravan format email adrese")]
    [StringLength(100, ErrorMessage = "Email ne smije biti duži od 100 znakova")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Lozinka je obavezna")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Lozinka mora imati između 4 i 100 znakova")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Potvrda lozinke je obavezna")]
    [Compare("Password", ErrorMessage = "Lozinke se ne podudaraju")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Broj telefona je obavezan")]
    [Phone(ErrorMessage = "Neispravan format broja telefona")]
    [StringLength(20, ErrorMessage = "Broj telefona ne smije biti duži od 20 znakova")]
    public string PhoneNumber { get; set; } = string.Empty;
} 