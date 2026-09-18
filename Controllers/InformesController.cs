using inmobiliaria.Models;
using inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace inmobiliaria.Controllers;

public class InformesController : Controller
{
    private const int TamPagina = 10;
    private readonly RepositorioInformes repositorio;
    private readonly RepositorioPropietarios repositorioPropietarios;

    public InformesController(
        RepositorioInformes repositorio,
        RepositorioPropietarios repositorioPropietarios)
    {
        this.repositorio = repositorio;
        this.repositorioPropietarios = repositorioPropietarios;
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
