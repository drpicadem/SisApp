using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.DTOs;

public class CreateReviewDto
{
    [Required]
    public int AppointmentId { get; set; }

    [Required(ErrorMessage = "ID frizera je obavezan")]
    public int BarberId { get; set; }

    [Required(ErrorMessage = "Ocjena je obavezna")]
    [Range(1, 5, ErrorMessage = "Ocjena mora biti između 1 i 5")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Komentar je obavezan")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Komentar mora imati između 10 i 500 znakova")]
    public string Comment { get; set; } = string.Empty;
} 