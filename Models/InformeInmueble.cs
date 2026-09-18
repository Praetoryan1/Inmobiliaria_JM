namespace inmobiliaria.Models;

public class InformeInmueble
{
    public Inmueble Inmueble { get; set; } = new();

    public int CantidadReservas { get; set; }
}

public enum TipoInformeInmueble
{
    Todos,
    PorPropietario,
    MasReservados,
    SinReservas
}

public class InformeInmueblesViewModel
{
    public TipoInformeInmueble Tipo { get; set; }

    public IList<InformeInmueble> Resultados { get; set; } = new List<InformeInmueble>();

    public string? Busqueda { get; set; }

    public bool? Disponible { get; set; }

    public int? IdPropietario { get; set; }

    public string? PropietarioSeleccionado { get; set; }

    public int Dias { get; set; } = 30;

    public int PaginaActual { get; set; }

    public int TotalPaginas { get; set; }

    public int CantidadTotal { get; set; }

    public string Accion => Tipo switch
    {
        TipoInformeInmueble.PorPropietario => "InmueblesPorPropietario",
        TipoInformeInmueble.MasReservados => "InmueblesMasReservados",
        TipoInformeInmueble.SinReservas => "InmueblesSinReservas",
        _ => "Inmuebles"
    };

    public string Titulo => Tipo switch
    {
        TipoInformeInmueble.PorPropietario => "Inmuebles por propietario",
        TipoInformeInmueble.MasReservados => "Inmuebles más reservados",
        TipoInformeInmueble.SinReservas => "Inmuebles sin reservas recientes",
        _ => "Inmuebles y propietarios"
    };

    public string Descripcion => Tipo switch
    {
        TipoInformeInmueble.PorPropietario =>
            "Propiedades que pertenecen al propietario seleccionado.",
        TipoInformeInmueble.MasReservados =>
            "Propiedades ordenadas por reservas iniciadas durante los últimos 365 días.",
        TipoInformeInmueble.SinReservas =>
            $"Propiedades sin ocupación reservada durante los últimos {Dias} días.",
        _ => "Todas las propiedades registradas, junto con su dueño y estado de oferta."
    };

    public string EtiquetaCantidad => Tipo switch
    {
        TipoInformeInmueble.MasReservados => "Reservas en 365 días",
        _ => "Reservas históricas"
    };
}
