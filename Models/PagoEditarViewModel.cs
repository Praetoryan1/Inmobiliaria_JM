using System.ComponentModel.DataAnnotations;

namespace inmobiliaria.Models;

public class PagoEditarViewModel
{
    public int IdPago { get; set; }

    public int IdReserva { get; set; }

    [Required(ErrorMessage = "El concepto es obligatorio.")]
    [StringLength(150, ErrorMessage = "El concepto no puede superar los 150 caracteres.")]
    public string Concepto { get; set; } = string.Empty;
}
