namespace inmobiliaria.Models;
using System.ComponentModel.DataAnnotations;

public class Usuario : Persona 
{
    [Required(ErrorMessage = "La contraseña es obligatoria"), DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string Role {get; set; } = "";

}
