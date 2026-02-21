using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.DTOs;

public class ReviewResponseDto
{
    [Required(ErrorMessage = "Odgovor je obavezan")]
    [StringLength(500, MinimumLength = 5, ErrorMessage = "Odgovor mora imati između 5 i 500 znakova")]
    public string Response { get; set; } = string.Empty;
}
