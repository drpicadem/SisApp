using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.Requests
{
    public class SalonUpdateRequest
    {
        [Required]
        [StringLength(100, MinimumLength = 2, ErrorMessage = "Naziv salona mora imati između 2 i 100 znakova.")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [StringLength(120, MinimumLength = 5, ErrorMessage = "Adresa mora imati između 5 i 120 znakova.")]
        public string Address { get; set; } = string.Empty;

        [Required]
        public int CityId { get; set; }

        [Required]
        [RegularExpression(@"^[A-Za-z0-9\- ]{3,10}$", ErrorMessage = "Poštanski broj mora imati 3-10 znakova (slova, brojevi, razmak ili -).")]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [RegularExpression(@"^\+?[0-9]{6,15}$", ErrorMessage = "Telefon mora biti u formatu +38761111222 ili 061111222 (6-15 cifara).")]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(255)]
        [RegularExpression(@"^https?:\/\/.+", ErrorMessage = "Web stranica mora početi sa http:// ili https://")]
        public string? Website { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? BusinessHours { get; set; }
        public string? Amenities { get; set; }
        public string? SocialMedia { get; set; }
    }
}
