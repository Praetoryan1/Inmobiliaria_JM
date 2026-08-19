namespace inmobiliaria.Models;
using System.ComponentModel.DataAnnotations;

public class Persona
{
    [Key]
    public int Id { get; set; }
    [Required]
    public string Nombre { get; set; } = "";
    [Required]
    public string Apellido { get; set; } = "";
    [Required]
    public string Dni { get; set; } = "";
    [Display(Name = "Teléfono")]
    public string Telefono { get; set; } = "";
    [Required, EmailAddress]
    public string Email { get; set; } = "";

}
