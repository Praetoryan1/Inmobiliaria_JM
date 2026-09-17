using System.ComponentModel.DataAnnotations;

namespace inmobiliaria.Models;

public class Pago
{
    [Key]
    public int IdPago { get; set; }

    public int IdReserva { get; set; }

    [Required(ErrorMessage = "El concepto es obligatorio.")]
    [StringLength(150, ErrorMessage = "El concepto no puede superar los 150 caracteres.")]
    public string Concepto { get; set; } = string.Empty;

    [DataType(DataType.Date)]
    [Display(Name = "Fecha de pago")]
    public DateTime FechaPago { get; set; }

    [Range(typeof(decimal), "0.01", "9999999999.99",
        ErrorMessage = "El importe debe ser mayor que cero.")]
    public decimal Importe { get; set; }

    public bool Anulado { get; set; }

    public int IdUsuarioCreador { get; set; }

    public int? IdUsuarioAnulador { get; set; }

    public DateTime? FechaAnulacion { get; set; }

    public Reserva? Reserva { get; set; }

    public Usuario? UsuarioCreador { get; set; }

    public Usuario? UsuarioAnulador { get; set; }

    public string Estado => Anulado ? "Anulado" : "Registrado";
}
