using System.ComponentModel.DataAnnotations;

namespace inmobiliaria.Models;

public class Inmueble
{
    [Key]
    public int IdInmueble { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un propietario.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un propietario.")]
    [Display(Name = "Propietario")]
    public int IdPropietario { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un tipo de inmueble.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un tipo de inmueble.")]
    [Display(Name = "Tipo de inmueble")]
    public int IdTipoInmueble { get; set; }

    [Required(ErrorMessage = "La dirección es obligatoria.")]
    [Display(Name = "Dirección")]
    [StringLength(200, ErrorMessage = "La dirección no puede superar los 200 caracteres.")]
    public string Direccion { get; set; } = string.Empty;

    [Required(ErrorMessage = "El cupo es obligatorio.")]
    [Range(1, 100, ErrorMessage = "El cupo debe estar entre 1 y 100 personas.")]
    public int Cupo { get; set; }

    [Required(ErrorMessage = "Las coordenadas son obligatorias.")]
    [StringLength(100, ErrorMessage = "Las coordenadas no pueden superar los 100 caracteres.")]
    public string Coordenadas { get; set; } = string.Empty;

    [Required(ErrorMessage = "El precio por día es obligatorio.")]
    [Display(Name = "Precio por día")]
    [Range(typeof(decimal), "0.01", "9999999999.99",
        ErrorMessage = "El precio por día debe ser mayor que cero.")]
    public decimal PrecioDia { get; set; }

    public bool Disponible { get; set; } = true;

    [Display(Name = "Imagen de portada")]
    public string? ImagenPortada { get; set; }

    [Display(Name = "Imagen de portada")]
    public IFormFile? ImagenArchivo { get; set; }

    public Propietario? Propietario { get; set; }

    public TipoInmueble? TipoInmueble { get; set; }
}
