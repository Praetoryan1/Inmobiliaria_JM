using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace inmobiliaria.Models;

public class RenovacionReservaViewModel : IValidatableObject
{
    public int IdReservaOrigen { get; set; }

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Nueva fecha desde")]
    public DateTime FechaDesde { get; set; }

    [Required(ErrorMessage = "La fecha de finalización es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Nueva fecha hasta")]
    public DateTime FechaHasta { get; set; }

    [Required(ErrorMessage = "El monto por día es obligatorio.")]
    [Display(Name = "Nuevo monto por día")]
    [Range(typeof(decimal), "0.01", "9999999999.99",
        ErrorMessage = "El monto por día debe ser mayor que cero.")]
    public decimal MontoDia { get; set; }

    [ValidateNever]
    public Reserva ReservaOrigen { get; set; } = null!;

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
