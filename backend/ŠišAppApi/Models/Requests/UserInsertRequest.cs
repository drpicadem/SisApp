using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.Requests
{
    public class UserInsertRequest
    {
        [Required(ErrorMessage = "Korisničko ime je obavezno.")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email je obavezan.")]
        [EmailAddress(ErrorMessage = "Neispravan format email adrese.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Ime je obavezno.")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Prezime je obavezno.")]
        public string LastName { get; set; } = string.Empty;

        public string? PhoneNumber { get; set; }

        public string? Role { get; set; } = "User";

        [Required(ErrorMessage = "Lozinka je obavezna.")]
        public string Password { get; set; } = string.Empty;
    }
}
