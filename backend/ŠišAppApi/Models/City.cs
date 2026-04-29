using System.ComponentModel.DataAnnotations;

namespace ŠišAppApi.Models;

public class City
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public virtual ICollection<Salon> Salons { get; set; } = new List<Salon>();
}
