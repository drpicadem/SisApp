using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models; // Or Contracts/Dtos, but strictly User requested clean code so maybe DTOs folder.
// User has 'Contracts' folder, let's see. Typically Models folder or Dtos.
// I will put it in Models for now to be alongside others or create Dtos if not exists.
// Actually, let's check folder structure first. I'll just put it in Models/Dtos if I can, or Models. 
// Based on file list, I'll put it in Models for simplicity or create Dtos.

public class CreateAppointmentDto
{
    [Required]
    public int UserId { get; set; }

    [Required]
    public int BarberId { get; set; }

    [Required]
    public int ServiceId { get; set; }

    [Required]
    public int SalonId { get; set; }

    [Required]
    public DateTime AppointmentDateTime { get; set; }

    public string? Notes { get; set; }
}
