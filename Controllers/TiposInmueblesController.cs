using inmobiliaria.Models;
using inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Controllers;

public class TiposInmueblesController : Controller
{
    private const int TamPagina = 10;
    private readonly RepositorioTiposInmueble repositorio;
    private readonly ILogger<TiposInmueblesController> logger;

    public TiposInmueblesController(
        RepositorioTiposInmueble repositorio,
        ILogger<TiposInmueblesController> logger)
    {
        this.repositorio = repositorio;
        this.logger = logger;
    }

    public IActionResult Index(string? busqueda = null, int pagina = 1)
    {
        var cantidadTotal = repositorio.ObtenerCantidad(busqueda);
        var totalPaginas = Math.Max(
            1,
            (int)Math.Ceiling(cantidadTotal / (double)TamPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        ViewBag.Busqueda = busqueda;
        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;
        ViewBag.CantidadTotal = cantidadTotal;

        return View(repositorio.ObtenerLista(busqueda, pagina, TamPagina));
    }

    public IActionResult Details(int id)
    {
        var tipo = repositorio.ObtenerPorId(id);
        return tipo is null ? NotFound() : View(tipo);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create([Bind("Nombre")] TipoInmueble tipo)
    {
        if (!ModelState.IsValid)
        {
            return View(tipo);
        }

        try
        {
            repositorio.Alta(tipo);
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            ModelState.AddModelError(
                nameof(TipoInmueble.Nombre),
                "Ya existe un tipo de inmueble con ese nombre.");
            return View(tipo);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al crear un tipo de inmueble.");
            ModelState.AddModelError(
                string.Empty,
                "No se pudo crear el tipo de inmueble. Intente nuevamente.");
            return View(tipo);
        }

        TempData["Mensaje"] = "El tipo de inmueble se creó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var tipo = repositorio.ObtenerPorId(id);
        return tipo is null ? NotFound() : View(tipo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(
        int id,
        [Bind("IdTipoInmueble,Nombre")] TipoInmueble tipo)
    {
        if (id != tipo.IdTipoInmueble)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(tipo);
        }

        try
        {
            if (!repositorio.Modificacion(tipo))
            {
                return NotFound();
            }
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            ModelState.AddModelError(
                nameof(TipoInmueble.Nombre),
                "Ya existe un tipo de inmueble con ese nombre.");
            return View(tipo);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al actualizar el tipo de inmueble {IdTipoInmueble}.",
                tipo.IdTipoInmueble);
            ModelState.AddModelError(
                string.Empty,
                "No se pudo actualizar el tipo de inmueble. Intente nuevamente.");
            return View(tipo);
        }

        TempData["Mensaje"] = "El tipo de inmueble se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var tipo = repositorio.ObtenerPorId(id);
        return tipo is null ? NotFound() : View(tipo);
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
                "No se puede eliminar el tipo porque está asignado a uno o más inmuebles.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al eliminar el tipo de inmueble {IdTipoInmueble}.",
                id);
            TempData["Error"] =
                "No se pudo eliminar el tipo de inmueble. Intente nuevamente.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Mensaje"] = "El tipo de inmueble se eliminó correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
