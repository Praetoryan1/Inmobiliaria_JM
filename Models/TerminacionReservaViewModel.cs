using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace inmobiliaria.Models;

public class TerminacionReservaViewModel
{
    public int IdReserva { get; set; }

    [Required(ErrorMessage = "La fecha de terminación es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha efectiva de terminación")]
    public DateTime FechaTerminacion { get; set; }

    [ValidateNever]
    public Reserva Reserva { get; set; } = null!;

    public int DiasOriginales { get; set; }

    public int DiasCumplidos { get; set; }

    public int DiasRestantes { get; set; }

    public decimal PorcentajeMulta { get; set; }

    public decimal MontoMulta { get; set; }
}
