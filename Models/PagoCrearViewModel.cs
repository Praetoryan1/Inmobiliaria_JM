using System.ComponentModel.DataAnnotations;

namespace inmobiliaria.Models;

public class PagoCrearViewModel
{
    [Range(1, int.MaxValue, ErrorMessage = "La reserva seleccionada no es válida.")]
    public int IdReserva { get; set; }

    [Required(ErrorMessage = "El concepto es obligatorio.")]
    [StringLength(150, ErrorMessage = "El concepto no puede superar los 150 caracteres.")]
    public string Concepto { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de pago es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de pago")]
    public DateTime FechaPago { get; set; }

    [Required(ErrorMessage = "El importe es obligatorio.")]
    [Range(typeof(decimal), "0.01", "9999999999.99",
        ErrorMessage = "El importe debe ser mayor que cero.")]
    public decimal Importe { get; set; }
}
