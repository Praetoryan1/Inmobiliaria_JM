namespace inmobiliaria.Models;

public enum TipoInformeReserva
{
    Vigentes,
    PorFinalizar
}

public class InformeReservasViewModel
{
    public TipoInformeReserva Tipo { get; set; }

    public IList<Reserva> Resultados { get; set; } = new List<Reserva>();

    public string? Busqueda { get; set; }

    public int Dias { get; set; } = 30;

    public int PaginaActual { get; set; }

    public int TotalPaginas { get; set; }

    public int CantidadTotal { get; set; }

    public string Accion => Tipo == TipoInformeReserva.PorFinalizar
        ? "ReservasPorFinalizar"
        : "ReservasVigentes";

    public string Titulo => Tipo == TipoInformeReserva.PorFinalizar
        ? "Reservas próximas a finalizar"
        : "Reservas vigentes";

    public string Descripcion => Tipo == TipoInformeReserva.PorFinalizar
        ? $"Reservas cuya fecha efectiva de finalización ocurre dentro de los próximos {Dias} días."
        : "Alquileres activos según sus fechas de inicio y finalización efectiva.";
}

public class InformeDisponibilidadViewModel
{
    public IList<InformeInmueble> Resultados { get; set; } = new List<InformeInmueble>();

    public DateTime FechaDesde { get; set; }

    public DateTime FechaHasta { get; set; }

    public string? Busqueda { get; set; }

    public string? Error { get; set; }

    public int PaginaActual { get; set; }

    public int TotalPaginas { get; set; }

    public int CantidadTotal { get; set; }
}
