using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.Requests
{
    public class SalonInsertRequest
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [MaxLength(255)]
        public string Address { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string City { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string PostalCode { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Country { get; set; } = string.Empty;

        [Required]
        [MaxLength(20)]
        public string Phone { get; set; } = string.Empty;

        [MaxLength(100)]
        [EmailAddress]
        public string? Email { get; set; }

        [MaxLength(255)]
        public string? Website { get; set; }

        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? BusinessHours { get; set; }
        public string? Amenities { get; set; }
        public string? SocialMedia { get; set; }
    }
}
