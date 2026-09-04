using System.ComponentModel.DataAnnotations;

namespace inmobiliaria.Models;

public class Reserva : IValidatableObject
{
    [Key]
    public int IdReserva { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un inmueble.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un inmueble.")]
    [Display(Name = "Inmueble")]
    public int IdInmueble { get; set; }

    [Required(ErrorMessage = "Debe seleccionar un inquilino.")]
    [Range(1, int.MaxValue, ErrorMessage = "Debe seleccionar un inquilino.")]
    [Display(Name = "Inquilino")]
    public int IdInquilino { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha desde")]
    public DateTime FechaDesde { get; set; }

    [Required(ErrorMessage = "La fecha de finalización es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha hasta")]
    public DateTime FechaHasta { get; set; }

    [Required(ErrorMessage = "El monto por día es obligatorio.")]
    [Display(Name = "Monto por día")]
    [Range(typeof(decimal), "0.01", "9999999999.99",
        ErrorMessage = "El monto por día debe ser mayor que cero.")]
    public decimal MontoDia { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "Terminación anticipada")]
    public DateTime? FechaTerminacionAnticipada { get; set; }

    [Display(Name = "Monto de multa")]
    public decimal? MontoMulta { get; set; }

    public Inmueble? Inmueble { get; set; }

    public Inquilino? Inquilino { get; set; }

    public string Estado
    {
        get
        {
            var hoy = DateTime.Today;
            var finEfectivo = FechaTerminacionAnticipada?.Date ?? FechaHasta.Date;

            if (FechaDesde.Date > hoy)
            {
                return "Pendiente";
            }

            if (finEfectivo < hoy)
            {
                return FechaTerminacionAnticipada.HasValue
                    ? "Finalizada anticipadamente"
                    : "Finalizada";
            }

            return FechaTerminacionAnticipada.HasValue
                ? "Vigente hasta terminación"
                : "Vigente";
        }
    }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (FechaHasta.Date <= FechaDesde.Date)
        {
            yield return new ValidationResult(
                "La fecha hasta debe ser posterior a la fecha desde.",
                new[] { nameof(FechaHasta) });
        }
    }
}
