using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.Requests
{
    public class UserInsertRequest
    {
        [Required(ErrorMessage = "Korisničko ime je obavezno.")]
        [StringLength(30, MinimumLength = 3, ErrorMessage = "Korisničko ime mora imati između 3 i 30 znakova.")]
        [RegularExpression(@"^[a-zA-Z0-9._-]+$", ErrorMessage = "Korisničko ime može sadržavati samo slova, brojeve i znakove . _ - bez razmaka.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Neispravan format email adrese.")]
        [StringLength(100, ErrorMessage = "Email ne smije biti duži od 100 znakova.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ime je obavezno.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Ime mora imati između 2 i 50 znakova.")]
        [RegularExpression(@"^[A-Za-zČčĆćŠšĐđŽž\s\-']+$", ErrorMessage = "Ime može sadržavati samo slova, razmak, crticu i apostrof.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Prezime je obavezno.")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Prezime mora imati između 2 i 50 znakova.")]
        [RegularExpression(@"^[A-Za-zČčĆćŠšĐđŽž\s\-']+$", ErrorMessage = "Prezime može sadržavati samo slova, razmak, crticu i apostrof.")]
        public string LastName { get; set; } = string.Empty;

        [RegularExpression(@"^\+?[0-9]{6,15}$", ErrorMessage = "Telefon mora biti u formatu +38761111222 ili 061111222 (6-15 cifara).")]
        public string? PhoneNumber { get; set; }

        public string? Role { get; set; } = "User";

        [Required(ErrorMessage = "Lozinka je obavezna.")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "Lozinka mora imati između 4 i 100 znakova.")]
        public string Password { get; set; } = string.Empty;
    }
}
