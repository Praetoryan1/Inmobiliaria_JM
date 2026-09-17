namespace inmobiliaria.Models;

public class PagoListadoViewModel
{
    public Reserva Reserva { get; set; } = null!;

    public IList<Pago> Pagos { get; set; } = new List<Pago>();

    public PagoCrearViewModel NuevoPago { get; set; } = new();

    public string Busqueda { get; set; } = string.Empty;

    public int PaginaActual { get; set; }

    public int TotalPaginas { get; set; }

    public int CantidadTotal { get; set; }

    public decimal TotalRegistrado { get; set; }
}
