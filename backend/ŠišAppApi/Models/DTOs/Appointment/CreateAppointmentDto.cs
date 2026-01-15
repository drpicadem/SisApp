using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models.DTOs.Appointment;

public class CreateAppointmentDto
{
    [Required(ErrorMessage = "ID salona je obavezan")]
    public int SalonId { get; set; }

    [Required(ErrorMessage = "ID frizera je obavezan")]
    public int BarberId { get; set; }

    [Required(ErrorMessage = "ID usluge je obavezan")]
    public int ServiceId { get; set; }

    [Required(ErrorMessage = "Datum i vrijeme termina su obavezni")]
    [FutureDate(ErrorMessage = "Datum termina mora biti u budućnosti")]
    public DateTime AppointmentDateTime { get; set; }

    [StringLength(500, ErrorMessage = "Napomena ne smije biti duža od 500 znakova")]
    public string? Notes { get; set; }
}

public class FutureDateAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is DateTime dateTime)
        {
            return dateTime > DateTime.Now;
        }
        return false;
    }
} 