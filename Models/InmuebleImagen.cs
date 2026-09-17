using System.ComponentModel.DataAnnotations;

namespace inmobiliaria.Models;

public class InmuebleImagen
{
    [Key]
    public int IdInmuebleImagen { get; set; }

    public int IdInmueble { get; set; }

    [Required]
    [StringLength(255)]
    public string Ruta { get; set; } = string.Empty;

    public Inmueble? Inmueble { get; set; }
}
