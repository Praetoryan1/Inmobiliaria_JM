using inmobiliaria.Models;
using inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Controllers;

public class PropietariosController : Controller
{
    private const int TamPagina = 10;
    private readonly RepositorioPropietarios repositorio;
    private readonly ILogger<PropietariosController> logger;

    public PropietariosController(
        RepositorioPropietarios repositorio,
        ILogger<PropietariosController> logger)
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
        var propietario = repositorio.ObtenerPorId(id);
        return propietario is null ? NotFound() : View(propietario);
    }

    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(
        [Bind("Dni,Nombre,Apellido,Telefono,Email")] Propietario propietario)
    {
        if (!ModelState.IsValid)
        {
            return View(propietario);
        }

        try
        {
            repositorio.Alta(propietario);
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            ModelState.AddModelError(
                string.Empty,
                "Ya existe un propietario con el mismo DNI o email.");
            return View(propietario);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al crear un propietario.");
            ModelState.AddModelError(
                string.Empty,
                "No se pudo crear el propietario. Intente nuevamente.");
            return View(propietario);
        }

        TempData["Mensaje"] = "El propietario se creó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var propietario = repositorio.ObtenerPorId(id);
        return propietario is null ? NotFound() : View(propietario);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(
        int id,
        [Bind("IdPropietario,Dni,Nombre,Apellido,Telefono,Email")]
        Propietario propietario)
    {
        if (id != propietario.IdPropietario)
        {
            return NotFound();
        }

        if (!ModelState.IsValid)
        {
            return View(propietario);
        }

        try
        {
            if (!repositorio.Modificacion(propietario))
            {
                return NotFound();
            }
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            ModelState.AddModelError(
                string.Empty,
                "Ya existe un propietario con el mismo DNI o email.");
            return View(propietario);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al actualizar el propietario {IdPropietario}.",
                propietario.IdPropietario);
            ModelState.AddModelError(
                string.Empty,
                "No se pudo actualizar el propietario. Intente nuevamente.");
            return View(propietario);
        }

        TempData["Mensaje"] = "El propietario se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var propietario = repositorio.ObtenerPorId(id);
        return propietario is null ? NotFound() : View(propietario);
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
                "No se puede eliminar el propietario porque tiene registros relacionados.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al eliminar el propietario {IdPropietario}.",
                id);
            TempData["Error"] =
                "No se pudo eliminar el propietario. Intente nuevamente.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Mensaje"] = "El propietario se eliminó correctamente.";
        return RedirectToAction(nameof(Index));
    }
}
