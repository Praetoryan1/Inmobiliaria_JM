using System.ComponentModel.DataAnnotations;

namespace inmobiliaria.Models;

public class UsuarioCrearViewModel
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

    [Required(ErrorMessage = "El rol es obligatorio.")]
    public string Rol { get; set; } = RolesUsuario.Empleado;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8,
        ErrorMessage = "La contraseña debe tener entre 8 y 100 caracteres.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe confirmar la contraseña.")]
    [Compare(nameof(Password), ErrorMessage = "Las contraseñas no coinciden.")]
    [DataType(DataType.Password)]
    public string ConfirmarPassword { get; set; } = string.Empty;
}
