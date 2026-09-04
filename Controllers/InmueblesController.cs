using inmobiliaria.Models;
using inmobiliaria.Repositories;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Controllers;

public class InmueblesController : Controller
{
    private const int TamPagina = 10;
    private const long TamanoMaximoImagen = 5 * 1024 * 1024;
    private static readonly HashSet<string> ExtensionesPermitidas =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly RepositorioInmuebles repositorio;
    private readonly RepositorioPropietarios repositorioPropietarios;
    private readonly RepositorioTiposInmueble repositorioTipos;
    private readonly IWebHostEnvironment environment;
    private readonly ILogger<InmueblesController> logger;

    public InmueblesController(
        RepositorioInmuebles repositorio,
        RepositorioPropietarios repositorioPropietarios,
        RepositorioTiposInmueble repositorioTipos,
        IWebHostEnvironment environment,
        ILogger<InmueblesController> logger)
    {
        this.repositorio = repositorio;
        this.repositorioPropietarios = repositorioPropietarios;
        this.repositorioTipos = repositorioTipos;
        this.environment = environment;
        this.logger = logger;
    }

    public IActionResult Index(
        string? busqueda = null,
        bool? disponible = null,
        int pagina = 1)
    {
        var cantidadTotal = repositorio.ObtenerCantidad(busqueda, disponible);
        var totalPaginas = Math.Max(
            1,
            (int)Math.Ceiling(cantidadTotal / (double)TamPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        ViewBag.Busqueda = busqueda;
        ViewBag.Disponible = disponible;
        ViewBag.PaginaActual = pagina;
        ViewBag.TotalPaginas = totalPaginas;
        ViewBag.CantidadTotal = cantidadTotal;

        return View(repositorio.ObtenerLista(
            busqueda,
            disponible,
            pagina,
            TamPagina));
    }

    public IActionResult Details(int id)
    {
        var inmueble = repositorio.ObtenerPorId(id);
        return inmueble is null ? NotFound() : View(inmueble);
    }

    public IActionResult Create()
    {
        PrepararFormulario();
        return View(new Inmueble { Disponible = true });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("IdPropietario,IdTipoInmueble,Direccion,Cupo,Coordenadas,PrecioDia,Disponible,ImagenArchivo")]
        Inmueble inmueble)
    {
        ValidarImagen(inmueble.ImagenArchivo);
        if (!ModelState.IsValid)
        {
            PrepararFormulario(inmueble);
            return View(inmueble);
        }

        string? imagenNueva = null;
        try
        {
            if (inmueble.ImagenArchivo is not null)
            {
                imagenNueva = await GuardarImagen(inmueble.ImagenArchivo);
                inmueble.ImagenPortada = imagenNueva;
            }

            repositorio.Alta(inmueble);
        }
        catch (MySqlException exception) when (exception.Number == 1452)
        {
            EliminarImagen(imagenNueva);
            ModelState.AddModelError(
                string.Empty,
                "El propietario o el tipo seleccionado ya no existe.");
            PrepararFormulario(inmueble);
            return View(inmueble);
        }
        catch (Exception exception)
        {
            EliminarImagen(imagenNueva);
            logger.LogError(exception, "Error al crear un inmueble.");
            ModelState.AddModelError(
                string.Empty,
                "No se pudo crear el inmueble. Intente nuevamente.");
            PrepararFormulario(inmueble);
            return View(inmueble);
        }

        TempData["Mensaje"] = "El inmueble se creó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var inmueble = repositorio.ObtenerPorId(id);
        if (inmueble is null)
        {
            return NotFound();
        }

        PrepararFormulario(inmueble);
        return View(inmueble);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        [Bind("IdInmueble,IdPropietario,IdTipoInmueble,Direccion,Cupo,Coordenadas,PrecioDia,Disponible,ImagenArchivo")]
        Inmueble inmueble)
    {
        if (id != inmueble.IdInmueble)
        {
            return NotFound();
        }

        var inmuebleActual = repositorio.ObtenerPorId(id);
        if (inmuebleActual is null)
        {
            return NotFound();
        }

        inmueble.ImagenPortada = inmuebleActual.ImagenPortada;
        ValidarImagen(inmueble.ImagenArchivo);
        if (!ModelState.IsValid)
        {
            PrepararFormulario(inmueble);
            return View(inmueble);
        }

        string? imagenNueva = null;
        try
        {
            if (inmueble.ImagenArchivo is not null)
            {
                imagenNueva = await GuardarImagen(inmueble.ImagenArchivo);
                inmueble.ImagenPortada = imagenNueva;
            }

            if (!repositorio.Modificacion(inmueble))
            {
                EliminarImagen(imagenNueva);
                return NotFound();
            }

            if (imagenNueva is not null)
            {
                EliminarImagen(inmuebleActual.ImagenPortada);
            }
        }
        catch (MySqlException exception) when (exception.Number == 1452)
        {
            EliminarImagen(imagenNueva);
            inmueble.ImagenPortada = inmuebleActual.ImagenPortada;
            ModelState.AddModelError(
                string.Empty,
                "El propietario o el tipo seleccionado ya no existe.");
            PrepararFormulario(inmueble);
            return View(inmueble);
        }
        catch (Exception exception)
        {
            EliminarImagen(imagenNueva);
            inmueble.ImagenPortada = inmuebleActual.ImagenPortada;
            logger.LogError(
                exception,
                "Error al actualizar el inmueble {IdInmueble}.",
                inmueble.IdInmueble);
            ModelState.AddModelError(
                string.Empty,
                "No se pudo actualizar el inmueble. Intente nuevamente.");
            PrepararFormulario(inmueble);
            return View(inmueble);
        }

        TempData["Mensaje"] = "El inmueble se actualizó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Delete(int id)
    {
        var inmueble = repositorio.ObtenerPorId(id);
        return inmueble is null ? NotFound() : View(inmueble);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var inmueble = repositorio.ObtenerPorId(id);
        if (inmueble is null)
        {
            return NotFound();
        }

        try
        {
            if (!repositorio.Baja(id))
            {
                return NotFound();
            }

            EliminarImagen(inmueble.ImagenPortada);
        }
        catch (MySqlException exception) when (exception.Number == 1451)
        {
            TempData["Error"] =
                "No se puede eliminar el inmueble porque tiene reservas relacionadas.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al eliminar el inmueble {IdInmueble}.",
                id);
            TempData["Error"] =
                "No se pudo eliminar el inmueble. Intente nuevamente.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Mensaje"] = "El inmueble se eliminó correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult BuscarPropietarios(string? termino)
    {
        var resultados = repositorioPropietarios
            .ObtenerLista(termino, 1, 10)
            .Select(p => new
            {
                id = p.IdPropietario,
                texto = $"{p.Apellido}, {p.Nombre} · DNI {p.Dni}"
            });

        return Json(resultados);
    }

    [HttpGet]
    public IActionResult BuscarTipos(string? termino)
    {
        var resultados = repositorioTipos
            .ObtenerLista(termino, 1, 10)
            .Select(t => new
            {
                id = t.IdTipoInmueble,
                texto = t.Nombre
            });

        return Json(resultados);
    }

    private void PrepararFormulario(Inmueble? inmueble = null)
    {
        var propietario = inmueble?.IdPropietario > 0
            ? repositorioPropietarios.ObtenerPorId(inmueble.IdPropietario)
            : null;
        var tipo = inmueble?.IdTipoInmueble > 0
            ? repositorioTipos.ObtenerPorId(inmueble.IdTipoInmueble)
            : null;

        ViewBag.PropietarioSeleccionado = propietario is null
            ? string.Empty
            : $"{propietario.Apellido}, {propietario.Nombre} · DNI {propietario.Dni}";
        ViewBag.TipoSeleccionado = tipo?.Nombre ?? string.Empty;
    }

    private void ValidarImagen(IFormFile? archivo)
    {
        if (archivo is null)
        {
            return;
        }

        if (archivo.Length == 0)
        {
            ModelState.AddModelError(
                nameof(Inmueble.ImagenArchivo),
                "El archivo de imagen está vacío.");
            return;
        }

        if (archivo.Length > TamanoMaximoImagen)
        {
            ModelState.AddModelError(
                nameof(Inmueble.ImagenArchivo),
                "La imagen no puede superar los 5 MB.");
        }

        var extension = Path.GetExtension(archivo.FileName);
        if (!ExtensionesPermitidas.Contains(extension))
        {
            ModelState.AddModelError(
                nameof(Inmueble.ImagenArchivo),
                "Solo se permiten imágenes JPG, PNG o WEBP.");
        }
    }

    private async Task<string> GuardarImagen(IFormFile archivo)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
        var carpeta = Path.Combine(environment.WebRootPath, "uploads", "inmuebles");
        Directory.CreateDirectory(carpeta);

        var rutaFisica = Path.Combine(carpeta, nombreArchivo);
        await using var stream = new FileStream(rutaFisica, FileMode.CreateNew);
        await archivo.CopyToAsync(stream);

        return $"/uploads/inmuebles/{nombreArchivo}";
    }

    private void EliminarImagen(string? rutaPublica)
    {
        if (string.IsNullOrWhiteSpace(rutaPublica))
        {
            return;
        }

        var nombreArchivo = Path.GetFileName(rutaPublica);
        var carpeta = Path.GetFullPath(
            Path.Combine(environment.WebRootPath, "uploads", "inmuebles"));
        var rutaFisica = Path.GetFullPath(Path.Combine(carpeta, nombreArchivo));

        if (!rutaFisica.StartsWith(
                carpeta + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        if (System.IO.File.Exists(rutaFisica))
        {
            System.IO.File.Delete(rutaFisica);
        }
    }
}
