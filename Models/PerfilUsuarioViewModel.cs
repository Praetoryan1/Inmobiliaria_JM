using System.ComponentModel.DataAnnotations;

namespace inmobiliaria.Models;

public class PerfilUsuarioViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio.")]
    [StringLength(100, ErrorMessage = "El apellido no puede superar los 100 caracteres.")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio.")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido.")]
    [StringLength(150, ErrorMessage = "El email no puede superar los 150 caracteres.")]
    public string Email { get; set; } = string.Empty;

    public string? AvatarActual { get; set; }

    [Display(Name = "Nuevo avatar")]
    public IFormFile? AvatarArchivo { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Contraseña actual")]
    public string? PasswordActual { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Nueva contraseña")]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "La nueva contraseña debe tener al menos 8 caracteres.")]
    public string? PasswordNueva { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirmar nueva contraseña")]
    [Compare(nameof(PasswordNueva), ErrorMessage = "Las contraseñas no coinciden.")]
    public string? ConfirmarPasswordNueva { get; set; }
}
