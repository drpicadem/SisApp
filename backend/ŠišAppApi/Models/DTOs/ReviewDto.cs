using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.DTOs;

public class ReviewDto
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ime korisnika je obavezno")]
    [StringLength(100, ErrorMessage = "Ime korisnika ne smije biti duže od 100 znakova")]
    public string UserName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ocjena je obavezna")]
    [Range(1, 5, ErrorMessage = "Ocjena mora biti između 1 i 5")]
    public int Rating { get; set; }

    [Required(ErrorMessage = "Komentar je obavezan")]
    [StringLength(500, MinimumLength = 10, ErrorMessage = "Komentar mora imati između 10 i 500 znakova")]
    public string Comment { get; set; } = string.Empty;

    [Required(ErrorMessage = "Datum kreiranja je obavezan")]
    public DateTime CreatedAt { get; set; }

    public int HelpfulCount { get; set; }
} 