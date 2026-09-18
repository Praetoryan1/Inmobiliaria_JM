using inmobiliaria.Models;
using inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace inmobiliaria.Controllers;

public class InformesController : Controller
{
    private const int TamPagina = 10;
    private readonly RepositorioInformes repositorio;
    private readonly RepositorioPropietarios repositorioPropietarios;
    private readonly RepositorioReservas repositorioReservas;

    public InformesController(
        RepositorioInformes repositorio,
        RepositorioPropietarios repositorioPropietarios,
        RepositorioReservas repositorioReservas)
    {
        this.repositorio = repositorio;
        this.repositorioPropietarios = repositorioPropietarios;
        this.repositorioReservas = repositorioReservas;
    }

    public IActionResult Index() => View();

    public IActionResult Inmuebles(
        string? busqueda = null,
        bool? disponible = null,
        int pagina = 1)
    {
        var cantidad = repositorio.ObtenerCantidadInmuebles(
            busqueda,
            disponible,
            null);
        pagina = NormalizarPagina(pagina, cantidad, out var totalPaginas);

        return View("Inmuebles", new InformeInmueblesViewModel
        {
            Tipo = TipoInformeInmueble.Todos,
            Resultados = repositorio.ObtenerInmuebles(
                busqueda,
                disponible,
                null,
                pagina,
                TamPagina),
            Busqueda = busqueda,
            Disponible = disponible,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            CantidadTotal = cantidad
        });
    }

    public IActionResult InmueblesPorPropietario(
        int? idPropietario = null,
        string? busqueda = null,
        int pagina = 1)
    {
        Propietario? propietario = null;
        if (idPropietario is > 0)
        {
            propietario = repositorioPropietarios.ObtenerPorId(idPropietario.Value);
            if (propietario is null)
            {
                return NotFound();
            }
        }
        else
        {
            idPropietario = null;
        }

        var cantidad = idPropietario.HasValue
            ? repositorio.ObtenerCantidadInmuebles(busqueda, null, idPropietario)
            : 0;
        pagina = NormalizarPagina(pagina, cantidad, out var totalPaginas);

        return View("Inmuebles", new InformeInmueblesViewModel
        {
            Tipo = TipoInformeInmueble.PorPropietario,
            Resultados = idPropietario.HasValue
                ? repositorio.ObtenerInmuebles(
                    busqueda,
                    null,
                    idPropietario,
                    pagina,
                    TamPagina)
                : new List<InformeInmueble>(),
            Busqueda = busqueda,
            IdPropietario = idPropietario,
            PropietarioSeleccionado = propietario is null
                ? null
                : $"{propietario.Apellido}, {propietario.Nombre} · DNI {propietario.Dni}",
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            CantidadTotal = cantidad
        });
    }

    public IActionResult InmueblesMasReservados(
        string? busqueda = null,
        int pagina = 1)
    {
        var cantidad = repositorio.ObtenerCantidadMasReservados(busqueda);
        pagina = NormalizarPagina(pagina, cantidad, out var totalPaginas);

        return View("Inmuebles", new InformeInmueblesViewModel
        {
            Tipo = TipoInformeInmueble.MasReservados,
            Resultados = repositorio.ObtenerMasReservados(
                busqueda,
                pagina,
                TamPagina),
            Busqueda = busqueda,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            CantidadTotal = cantidad
        });
    }

    public IActionResult InmueblesSinReservas(
        string? busqueda = null,
        int dias = 30,
        int pagina = 1)
    {
        dias = Math.Clamp(dias, 1, 3650);
        var cantidad = repositorio.ObtenerCantidadSinReservas(busqueda, dias);
        pagina = NormalizarPagina(pagina, cantidad, out var totalPaginas);

        return View("Inmuebles", new InformeInmueblesViewModel
        {
            Tipo = TipoInformeInmueble.SinReservas,
            Resultados = repositorio.ObtenerSinReservas(
                busqueda,
                dias,
                pagina,
                TamPagina),
            Busqueda = busqueda,
            Dias = dias,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            CantidadTotal = cantidad
        });
    }

    public IActionResult ReservasVigentes(
        string? busqueda = null,
        int pagina = 1)
    {
        var cantidad = repositorioReservas.ObtenerCantidad(busqueda, "vigente");
        pagina = NormalizarPagina(pagina, cantidad, out var totalPaginas);

        return View("Reservas", new InformeReservasViewModel
        {
            Tipo = TipoInformeReserva.Vigentes,
            Resultados = repositorioReservas.ObtenerLista(
                busqueda,
                "vigente",
                pagina,
                TamPagina),
            Busqueda = busqueda,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            CantidadTotal = cantidad
        });
    }

    public IActionResult ReservasPorFinalizar(
        string? busqueda = null,
        int dias = 30,
        int pagina = 1)
    {
        dias = Math.Clamp(dias, 1, 3650);
        var cantidad = repositorioReservas.ObtenerCantidadProximasAFinalizar(
            busqueda,
            dias);
        pagina = NormalizarPagina(pagina, cantidad, out var totalPaginas);

        return View("Reservas", new InformeReservasViewModel
        {
            Tipo = TipoInformeReserva.PorFinalizar,
            Resultados = repositorioReservas.ObtenerProximasAFinalizar(
                busqueda,
                dias,
                pagina,
                TamPagina),
            Busqueda = busqueda,
            Dias = dias,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            CantidadTotal = cantidad
        });
    }

    public IActionResult InmueblesDisponibles(
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null,
        string? busqueda = null,
        int pagina = 1)
    {
        var desde = fechaDesde?.Date ?? DateTime.Today.AddDays(1);
        var hasta = fechaHasta?.Date ?? desde.AddDays(1);
        if (hasta <= desde)
        {
            return View("Disponibilidad", new InformeDisponibilidadViewModel
            {
                FechaDesde = desde,
                FechaHasta = hasta,
                Busqueda = busqueda,
                Error = "La fecha hasta debe ser posterior a la fecha desde.",
                PaginaActual = 1,
                TotalPaginas = 1
            });
        }

        var cantidad = repositorio.ObtenerCantidadDisponiblesEntreFechas(
            desde,
            hasta,
            busqueda);
        pagina = NormalizarPagina(pagina, cantidad, out var totalPaginas);

        return View("Disponibilidad", new InformeDisponibilidadViewModel
        {
            Resultados = repositorio.ObtenerDisponiblesEntreFechas(
                desde,
                hasta,
                busqueda,
                pagina,
                TamPagina),
            FechaDesde = desde,
            FechaHasta = hasta,
            Busqueda = busqueda,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            CantidadTotal = cantidad
        });
    }

    private static int NormalizarPagina(
        int pagina,
        int cantidad,
        out int totalPaginas)
    {
        totalPaginas = Math.Max(
            1,
            (int)Math.Ceiling(cantidad / (double)TamPagina));
        return Math.Clamp(pagina, 1, totalPaginas);
    }
}
