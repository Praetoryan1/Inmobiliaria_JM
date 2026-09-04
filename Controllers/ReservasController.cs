using inmobiliaria.Models;
using inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Controllers;

public class ReservasController : Controller
{
    private const int TamPagina = 10;
    private static readonly HashSet<string> EstadosPermitidos =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "pendiente",
            "vigente",
            "finalizada",
            "anticipada"
        };

    private readonly RepositorioReservas repositorio;
    private readonly RepositorioInmuebles repositorioInmuebles;
    private readonly RepositorioInquilinos repositorioInquilinos;
    private readonly ILogger<ReservasController> logger;

    public ReservasController(
        RepositorioReservas repositorio,
        RepositorioInmuebles repositorioInmuebles,
        RepositorioInquilinos repositorioInquilinos,
        ILogger<ReservasController> logger)
    {
        this.repositorio = repositorio;
        this.repositorioInmuebles = repositorioInmuebles;
        this.repositorioInquilinos = repositorioInquilinos;
        this.logger = logger;
    }

    public IActionResult Index(
        string? busqueda = null,
        string? estado = null,
        int pagina = 1)
    {
        estado = NormalizarEstado(estado);
        var cantidadTotal = repositorio.ObtenerCantidad(busqueda, estado);
        var totalPaginas = Math.Max(
            1,
            (int)Math.Ceiling(cantidadTotal / (double)TamPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        ViewBag.Busqueda = busqueda;
        ViewBag.Estado = estado;
        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;
        ViewBag.CantidadTotal = cantidadTotal;

        return View(repositorio.ObtenerLista(busqueda, estado, pagina, TamPagina));
    }

    public IActionResult Details(int id)
    {
        var reserva = repositorio.ObtenerPorId(id);
        return reserva is null ? NotFound() : View(reserva);
    }

    public IActionResult Create()
    {
        PrepararFormulario();
        return View(new Reserva
        {
            FechaDesde = DateTime.Today.AddDays(1),
            FechaHasta = DateTime.Today.AddDays(2)
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(
        [Bind("IdInmueble,IdInquilino,FechaDesde,FechaHasta,MontoDia")]
        Reserva reserva)
    {
        ValidarRelacionesYDisponibilidad(reserva);
        if (!ModelState.IsValid)
        {
            PrepararFormulario(reserva);
            return View(reserva);
        }

        try
        {
            repositorio.Alta(reserva);
        }
        catch (MySqlException exception) when (exception.Number == 1452)
        {
            ModelState.AddModelError(
                string.Empty,
                "El inmueble o el inquilino seleccionado ya no existe.");
            PrepararFormulario(reserva);
            return View(reserva);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al crear una reserva.");
            ModelState.AddModelError(
                string.Empty,
                "No se pudo crear la reserva. Intente nuevamente.");
            PrepararFormulario(reserva);
            return View(reserva);
        }

        TempData["Mensaje"] = "La reserva se creó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var reserva = repositorio.ObtenerPorId(id);
        if (reserva is null)
        {
            return NotFound();
        }

        PrepararFormulario(reserva);
        return View(reserva);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(
        int id,
        [Bind("IdReserva,IdInmueble,IdInquilino,FechaDesde,FechaHasta,MontoDia")]
        Reserva reserva)
    {
        if (id != reserva.IdReserva)
        {
            return NotFound();
        }

        var reservaActual = repositorio.ObtenerPorId(id);
        if (reservaActual is null)
        {
            return NotFound();
        }

        ValidarRelacionesYDisponibilidad(reserva, id, reservaActual.IdInmueble);
        if (!ModelState.IsValid)
        {
            PrepararFormulario(reserva);
            return View(reserva);
        }

        try
        {
            if (!repositorio.Modificacion(reserva))
            {
                return NotFound();
            }
        }
        catch (MySqlException exception) when (exception.Number == 1452)
        {
            ModelState.AddModelError(
                string.Empty,
                "El inmueble o el inquilino seleccionado ya no existe.");
            PrepararFormulario(reserva);
            return View(reserva);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al actualizar la reserva {IdReserva}.",
                reserva.IdReserva);
            ModelState.AddModelError(
                string.Empty,
                "No se pudo actualizar la reserva. Intente nuevamente.");
            PrepararFormulario(reserva);
            return View(reserva);
        }

        TempData["Mensaje"] = "La reserva se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var reserva = repositorio.ObtenerPorId(id);
        return reserva is null ? NotFound() : View(reserva);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        try
        {
            if (!repositorio.Baja(id))
            {
                return NotFound();
            }
        }
        catch (MySqlException exception) when (exception.Number == 1451)
        {
            TempData["Error"] =
                "No se puede eliminar la reserva porque tiene pagos relacionados.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al eliminar la reserva {IdReserva}.",
                id);
            TempData["Error"] =
                "No se pudo eliminar la reserva. Intente nuevamente.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Mensaje"] = "La reserva se eliminó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult BuscarInmuebles(string? termino)
    {
        var resultados = repositorioInmuebles
            .ObtenerLista(termino, true, 1, 10)
            .Select(i => new
            {
                id = i.IdInmueble,
                texto = $"{i.Direccion} · {i.TipoInmueble?.Nombre}",
                precioDia = i.PrecioDia
            });

        return Json(resultados);
    }

    [HttpGet]
    public IActionResult BuscarInquilinos(string? termino)
    {
        var resultados = repositorioInquilinos
            .ObtenerLista(termino, 1, 10)
            .Select(i => new
            {
                id = i.IdInquilino,
                texto = $"{i.Apellido}, {i.Nombre} · DNI {i.Dni}"
            });

        return Json(resultados);
    }

    private void ValidarRelacionesYDisponibilidad(
        Reserva reserva,
        int? idReservaExcluir = null,
        int? idInmuebleOriginal = null)
    {
        var inmueble = reserva.IdInmueble > 0
            ? repositorioInmuebles.ObtenerPorId(reserva.IdInmueble)
            : null;

        if (reserva.IdInmueble > 0 && inmueble is null)
        {
            ModelState.AddModelError(
                nameof(Reserva.IdInmueble),
                "El inmueble seleccionado no existe.");
        }
        else if (inmueble is not null
            && !inmueble.Disponible
            && inmueble.IdInmueble != idInmuebleOriginal)
        {
            ModelState.AddModelError(
                nameof(Reserva.IdInmueble),
                "El inmueble seleccionado tiene su oferta suspendida.");
        }

        if (reserva.IdInquilino > 0
            && repositorioInquilinos.ObtenerPorId(reserva.IdInquilino) is null)
        {
            ModelState.AddModelError(
                nameof(Reserva.IdInquilino),
                "El inquilino seleccionado no existe.");
        }

        if (reserva.IdInmueble > 0
            && reserva.FechaHasta.Date > reserva.FechaDesde.Date
            && repositorio.ExisteSuperposicion(
                reserva.IdInmueble,
                reserva.FechaDesde,
                reserva.FechaHasta,
                idReservaExcluir))
        {
            ModelState.AddModelError(
                nameof(Reserva.FechaHasta),
                "El inmueble ya posee una reserva que se superpone con esas fechas.");
        }
    }

    private void PrepararFormulario(Reserva? reserva = null)
    {
        var inmueble = reserva?.IdInmueble > 0
            ? repositorioInmuebles.ObtenerPorId(reserva.IdInmueble)
            : null;
        var inquilino = reserva?.IdInquilino > 0
            ? repositorioInquilinos.ObtenerPorId(reserva.IdInquilino)
            : null;

        ViewBag.InmuebleSeleccionado = inmueble is null
            ? string.Empty
            : $"{inmueble.Direccion} · {inmueble.TipoInmueble?.Nombre}";
        ViewBag.InquilinoSeleccionado = inquilino is null
            ? string.Empty
            : $"{inquilino.Apellido}, {inquilino.Nombre} · DNI {inquilino.Dni}";
    }

    private static string? NormalizarEstado(string? estado) =>
        !string.IsNullOrWhiteSpace(estado) && EstadosPermitidos.Contains(estado)
            ? estado.ToLowerInvariant()
            : null;
}
