using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.DTOs.Salon;

public class CreateSalonDto
{
    [Required(ErrorMessage = "Naziv salona je obavezan")]
    [StringLength(100, MinimumLength = 3, ErrorMessage = "Naziv salona mora imati između 3 i 100 znakova")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Adresa je obavezna")]
    [StringLength(200, MinimumLength = 5, ErrorMessage = "Adresa mora imati između 5 i 200 znakova")]
    public string Address { get; set; } = string.Empty;

    [Required(ErrorMessage = "Grad je obavezan")]
    [StringLength(100, ErrorMessage = "Grad ne smije biti duži od 100 znakova")]
    public string City { get; set; } = string.Empty;

    [Required(ErrorMessage = "Poštanski broj je obavezan")]
    [RegularExpression(@"^\d{5}$", ErrorMessage = "Poštanski broj mora sadržavati točno 5 znamenki")]
    public string PostalCode { get; set; } = string.Empty;

    [Required(ErrorMessage = "Broj telefona je obavezan")]
    [Phone(ErrorMessage = "Neispravan format broja telefona")]
    [StringLength(20, ErrorMessage = "Broj telefona ne smije biti duži od 20 znakova")]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email je obavezan")]
    [EmailAddress(ErrorMessage = "Neispravan format email adrese")]
    [StringLength(100, ErrorMessage = "Email ne smije biti duži od 100 znakova")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Opis je obavezan")]
    [StringLength(1000, MinimumLength = 10, ErrorMessage = "Opis mora imati između 10 i 1000 znakova")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Radno vrijeme je obavezno")]
    public WorkingHoursDto WorkingHours { get; set; } = new();

    public List<string> Amenities { get; set; } = new();
}

public class WorkingHoursDto
{
    [Required(ErrorMessage = "Radno vrijeme za ponedjeljak je obavezno")]
    public DayHoursDto Monday { get; set; } = new();

    [Required(ErrorMessage = "Radno vrijeme za utorak je obavezno")]
    public DayHoursDto Tuesday { get; set; } = new();

    [Required(ErrorMessage = "Radno vrijeme za srijedu je obavezno")]
    public DayHoursDto Wednesday { get; set; } = new();

    [Required(ErrorMessage = "Radno vrijeme za četvrtak je obavezno")]
    public DayHoursDto Thursday { get; set; } = new();

    [Required(ErrorMessage = "Radno vrijeme za petak je obavezno")]
    public DayHoursDto Friday { get; set; } = new();

    [Required(ErrorMessage = "Radno vrijeme za subotu je obavezno")]
    public DayHoursDto Saturday { get; set; } = new();

    [Required(ErrorMessage = "Radno vrijeme za nedjelju je obavezno")]
    public DayHoursDto Sunday { get; set; } = new();
}

public class DayHoursDto
{
    [Required(ErrorMessage = "Vrijeme otvaranja je obavezno")]
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Neispravan format vremena (HH:mm)")]
    public string OpenTime { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vrijeme zatvaranja je obavezno")]
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Neispravan format vremena (HH:mm)")]
    public string CloseTime { get; set; } = string.Empty;

    public bool IsClosed { get; set; }
} 