using System.ComponentModel.DataAnnotations;

namespace inmobiliaria.Models;

public class Propietario
{
    [Key]
    public int IdPropietario { get; set; }

    [Required(ErrorMessage = "El DNI es obligatorio.")]
    [Display(Name = "DNI")]
    [StringLength(8, MinimumLength = 7,
        ErrorMessage = "El DNI debe tener entre 7 y 8 dígitos.")]
    [RegularExpression(@"^[0-9]+$", ErrorMessage = "El DNI solo puede contener números.")]
    public string Dni { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [Display(Name = "Teléfono")]
    [StringLength(30, ErrorMessage = "El teléfono no puede superar los 30 caracteres.")]
    public string? Telefono { get; set; }

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
    [StringLength(150, ErrorMessage = "El email no puede superar los 150 caracteres.")]
    public string Email { get; set; } = string.Empty;
}
