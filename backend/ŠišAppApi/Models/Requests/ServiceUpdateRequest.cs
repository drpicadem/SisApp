using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ŠišAppApi.Models.Requests
{
    public class ServiceUpdateRequest
    {
        [Required]
        [StringLength(80, MinimumLength = 2, ErrorMessage = "Naziv usluge mora imati između 2 i 80 znakova.")]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        [Required]
        [Range(1, 600, ErrorMessage = "Trajanje mora biti između 1 i 600 minuta.")]
        public int DurationMinutes { get; set; }

        [Required]
        [Column(TypeName = "decimal(10,2)")]
        [Range(typeof(decimal), "0.01", "1000", ErrorMessage = "Cijena mora biti između 0.01 i 1000 KM.")]
        public decimal Price { get; set; }

        public int? CategoryId { get; set; }
        public bool IsPopular { get; set; } = false;
        [Range(0, 10000, ErrorMessage = "Redoslijed prikaza mora biti između 0 i 10000.")]
        public int DisplayOrder { get; set; } = 0;
    }
}
