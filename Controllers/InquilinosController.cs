using inmobiliaria.Models;
using inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Controllers;

public class InquilinosController : Controller
{
    private const int TamPagina = 10;
    private readonly RepositorioInquilinos repositorio;
    private readonly ILogger<InquilinosController> logger;

    public InquilinosController(
        RepositorioInquilinos repositorio,
        ILogger<InquilinosController> logger)
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
        var inquilino = repositorio.ObtenerPorId(id);
        return inquilino is null ? NotFound() : View(inquilino);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(
        [Bind("Dni,Nombre,Apellido,Telefono,Email")] Inquilino inquilino)
    {
        if (!ModelState.IsValid)
        {
            return View(inquilino);
        }

        try
        {
            repositorio.Alta(inquilino);
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            ModelState.AddModelError(
                string.Empty,
                "Ya existe un inquilino con el mismo DNI o email.");
            return View(inquilino);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al crear un inquilino.");
            ModelState.AddModelError(
                string.Empty,
                "No se pudo crear el inquilino. Intente nuevamente.");
            return View(inquilino);
        }

        TempData["Mensaje"] = "El inquilino se creó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var inquilino = repositorio.ObtenerPorId(id);
        return inquilino is null ? NotFound() : View(inquilino);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(
        int id,
        [Bind("IdInquilino,Dni,Nombre,Apellido,Telefono,Email")]
        Inquilino inquilino)
    {
        if (id != inquilino.IdInquilino)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(inquilino);
        }

        try
        {
            if (!repositorio.Modificacion(inquilino))
            {
                return NotFound();
            }
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            ModelState.AddModelError(
                string.Empty,
                "Ya existe un inquilino con el mismo DNI o email.");
            return View(inquilino);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al actualizar el inquilino {IdInquilino}.",
                inquilino.IdInquilino);
            ModelState.AddModelError(
                string.Empty,
                "No se pudo actualizar el inquilino. Intente nuevamente.");
            return View(inquilino);
        }

        TempData["Mensaje"] = "El inquilino se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var inquilino = repositorio.ObtenerPorId(id);
        return inquilino is null ? NotFound() : View(inquilino);
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
                "No se puede eliminar el inquilino porque tiene registros relacionados.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al eliminar el inquilino {IdInquilino}.",
                id);
            TempData["Error"] =
                "No se pudo eliminar el inquilino. Intente nuevamente.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Mensaje"] = "El inquilino se eliminó correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
