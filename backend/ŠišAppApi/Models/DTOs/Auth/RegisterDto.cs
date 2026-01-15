using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.DTOs.Auth;

public class RegisterDto
{
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
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Lozinka mora imati između 6 i 100 znakova")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{6,}$",
        ErrorMessage = "Lozinka mora sadržavati najmanje jedno veliko slovo, jedno malo slovo, jedan broj i jedan poseban znak")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Potvrda lozinke je obavezna")]
    [Compare("Password", ErrorMessage = "Lozinke se ne podudaraju")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Broj telefona je obavezan")]
    [Phone(ErrorMessage = "Neispravan format broja telefona")]
    [StringLength(20, ErrorMessage = "Broj telefona ne smije biti duži od 20 znakova")]
    public string PhoneNumber { get; set; } = string.Empty;
} 