using inmobiliaria.Models;
using inmobiliaria.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Controllers;

public class InmueblesController : Controller
{
    private const int TamPagina = 10;
    private const int MaximoImagenesPorCarga = 10;
    private const long TamanoMaximoImagen = 5 * 1024 * 1024;
    private static readonly HashSet<string> ExtensionesPermitidas =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly RepositorioInmuebles repositorio;
    private readonly RepositorioPropietarios repositorioPropietarios;
    private readonly RepositorioTiposInmueble repositorioTipos;
    private readonly RepositorioInmuebleImagenes repositorioImagenes;
    private readonly IWebHostEnvironment environment;
    private readonly ILogger<InmueblesController> logger;

    public InmueblesController(
        RepositorioInmuebles repositorio,
        RepositorioPropietarios repositorioPropietarios,
        RepositorioTiposInmueble repositorioTipos,
        RepositorioInmuebleImagenes repositorioImagenes,
        IWebHostEnvironment environment,
        ILogger<InmueblesController> logger)
    {
        this.repositorio = repositorio;
        this.repositorioPropietarios = repositorioPropietarios;
        this.repositorioTipos = repositorioTipos;
        this.repositorioImagenes = repositorioImagenes;
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
        if (inmueble is null)
        {
            return NotFound();
        }

        inmueble.Imagenes = repositorioImagenes.ObtenerPorInmueble(id);
        return View(inmueble);
    }

    public IActionResult Create()
    {
        PrepararFormulario();
        return View(new Inmueble
        {
            Disponible = true,
            PorcentajeReserva = 20m
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        [Bind("IdPropietario,IdTipoInmueble,Direccion,Cupo,Coordenadas,PrecioDia,PorcentajeReserva,Disponible,ImagenArchivo")]
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
            EliminarArchivoImagen(imagenNueva);
            ModelState.AddModelError(
                string.Empty,
                "El propietario o el tipo seleccionado ya no existe.");
            PrepararFormulario(inmueble);
            return View(inmueble);
        }
        catch (Exception exception)
        {
            EliminarArchivoImagen(imagenNueva);
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
        [Bind("IdInmueble,IdPropietario,IdTipoInmueble,Direccion,Cupo,Coordenadas,PrecioDia,PorcentajeReserva,Disponible,ImagenArchivo")]
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
                EliminarArchivoImagen(imagenNueva);
                return NotFound();
            }

            if (imagenNueva is not null)
            {
                EliminarArchivoImagen(inmuebleActual.ImagenPortada);
            }
        }
        catch (MySqlException exception) when (exception.Number == 1452)
        {
            EliminarArchivoImagen(imagenNueva);
            inmueble.ImagenPortada = inmuebleActual.ImagenPortada;
            ModelState.AddModelError(
                string.Empty,
                "El propietario o el tipo seleccionado ya no existe.");
            PrepararFormulario(inmueble);
            return View(inmueble);
        }
        catch (Exception exception)
        {
            EliminarArchivoImagen(imagenNueva);
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

    [Authorize(Roles = RolesUsuario.Administrador)]
    public IActionResult Delete(int id)
    {
        var inmueble = repositorio.ObtenerPorId(id);
        return inmueble is null ? NotFound() : View(inmueble);
    }

    [Authorize(Roles = RolesUsuario.Administrador)]
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public IActionResult DeleteConfirmed(int id)
    {
        var inmueble = repositorio.ObtenerPorId(id);
        if (inmueble is null)
        {
            return NotFound();
        }

        var imagenes = repositorioImagenes.ObtenerPorInmueble(id);

        try
        {
            if (!repositorio.Baja(id))
            {
                return NotFound();
            }

            EliminarArchivoImagen(inmueble.ImagenPortada);
            foreach (var imagen in imagenes)
            {
                EliminarArchivoImagen(imagen.Ruta);
            }
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarImagenes(
        int id,
        List<IFormFile>? archivos)
    {
        if (repositorio.ObtenerPorId(id) is null)
        {
            return NotFound();
        }

        if (archivos is null || archivos.Count == 0)
        {
            TempData["Error"] = "Debe seleccionar al menos una imagen.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (archivos.Count > MaximoImagenesPorCarga)
        {
            TempData["Error"] =
                $"Se pueden cargar hasta {MaximoImagenesPorCarga} imágenes por vez.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var errores = archivos
            .Select(ObtenerErrorImagen)
            .Where(error => error is not null)
            .Distinct()
            .ToList();
        if (errores.Count > 0)
        {
            TempData["Error"] = string.Join(" ", errores);
            return RedirectToAction(nameof(Details), new { id });
        }

        var rutasNuevas = new List<string>();
        try
        {
            foreach (var archivo in archivos)
            {
                rutasNuevas.Add(await GuardarImagen(archivo));
            }

            repositorioImagenes.AltaVarias(id, rutasNuevas);
        }
        catch (Exception exception)
        {
            foreach (var ruta in rutasNuevas)
            {
                EliminarArchivoImagen(ruta);
            }

            logger.LogError(
                exception,
                "Error al agregar imágenes al inmueble {IdInmueble}.",
                id);
            TempData["Error"] =
                "No se pudieron guardar las imágenes. Intente nuevamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        TempData["Mensaje"] = archivos.Count == 1
            ? "La imagen se agregó correctamente."
            : $"Las {archivos.Count} imágenes se agregaron correctamente.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = RolesUsuario.Administrador)]
    public IActionResult EliminarImagen(int id)
    {
        var imagen = repositorioImagenes.ObtenerPorId(id);
        return imagen is null ? NotFound() : View(imagen);
    }

    [Authorize(Roles = RolesUsuario.Administrador)]
    [HttpPost, ActionName(nameof(EliminarImagen))]
    [ValidateAntiForgeryToken]
    public IActionResult EliminarImagenConfirmada(int id)
    {
        var imagen = repositorioImagenes.ObtenerPorId(id);
        if (imagen is null)
        {
            return NotFound();
        }

        try
        {
            if (!repositorioImagenes.Baja(id))
            {
                return NotFound();
            }

            EliminarArchivoImagen(imagen.Ruta);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al eliminar la imagen {IdInmuebleImagen}.",
                id);
            TempData["Error"] =
                "No se pudo eliminar la imagen. Intente nuevamente.";
            return RedirectToAction(
                nameof(Details),
                new { id = imagen.IdInmueble });
        }

        TempData["Mensaje"] = "La imagen se eliminó correctamente.";
        return RedirectToAction(nameof(Details), new { id = imagen.IdInmueble });
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

        var error = ObtenerErrorImagen(archivo);
        if (error is not null)
        {
            ModelState.AddModelError(
                nameof(Inmueble.ImagenArchivo),
                error);
        }
    }

    private static string? ObtenerErrorImagen(IFormFile archivo)
    {
        if (archivo.Length == 0)
        {
            return "Uno de los archivos de imagen está vacío.";
        }

        if (archivo.Length > TamanoMaximoImagen)
        {
            return "Cada imagen puede pesar como máximo 5 MB.";
        }

        var extension = Path.GetExtension(archivo.FileName);
        return ExtensionesPermitidas.Contains(extension)
            ? null
            : "Solo se permiten imágenes JPG, PNG o WEBP.";
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

    private void EliminarArchivoImagen(string? rutaPublica)
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
            try
            {
                System.IO.File.Delete(rutaFisica);
            }
            catch (Exception exception)
            {
                logger.LogWarning(
                    exception,
                    "No se pudo eliminar el archivo de imagen {RutaImagen}.",
                    rutaFisica);
            }
        }
    }
}
