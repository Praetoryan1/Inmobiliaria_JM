using System.ComponentModel.DataAnnotations;

namespace inmobiliaria.Models;

public class TipoInmueble
{
    [Key]
    public int IdTipoInmueble { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [Display(Name = "Tipo de inmueble")]
    [StringLength(80, ErrorMessage = "El nombre no puede superar los 80 caracteres.")]
    public string Nombre { get; set; } = string.Empty;
}
