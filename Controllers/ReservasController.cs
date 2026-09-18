using System.Globalization;
using System.Security.Claims;
using inmobiliaria.Models;
using inmobiliaria.Repositories;
using Microsoft.AspNetCore.Authorization;
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

    public IActionResult Create(
        int? idInmueble = null,
        DateTime? fechaDesde = null,
        DateTime? fechaHasta = null)
    {
        var desde = fechaDesde?.Date ?? DateTime.Today.AddDays(1);
        var hasta = fechaHasta?.Date ?? desde.AddDays(1);
        if (hasta <= desde)
        {
            hasta = desde.AddDays(1);
        }

        var reserva = new Reserva
        {
            FechaDesde = desde,
            FechaHasta = hasta
        };

        if (idInmueble is > 0)
        {
            var inmueble = repositorioInmuebles.ObtenerPorId(idInmueble.Value);
            if (inmueble is null || !inmueble.Disponible)
            {
                return NotFound();
            }

            reserva.IdInmueble = inmueble.IdInmueble;
            reserva.MontoDia = inmueble.PrecioDia;
        }

        PrepararFormulario(reserva, mostrarPagoInicial: true);
        return View(reserva);
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
            PrepararFormulario(reserva, mostrarPagoInicial: true);
            return View(reserva);
        }

        var inmueble = repositorioInmuebles.ObtenerPorId(reserva.IdInmueble);
        if (inmueble is null)
        {
            return NotFound();
        }

        var idUsuario = ObtenerIdUsuarioActual();
        if (!idUsuario.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            repositorio.Alta(
                reserva,
                idUsuario.Value,
                inmueble.PorcentajeReserva);
        }
        catch (MySqlException exception) when (exception.Number == 1452)
        {
            ModelState.AddModelError(
                string.Empty,
                "El inmueble o el inquilino seleccionado ya no existe.");
            PrepararFormulario(reserva, mostrarPagoInicial: true);
            return View(reserva);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al crear una reserva.");
            ModelState.AddModelError(
                string.Empty,
                "No se pudo crear la reserva. Intente nuevamente.");
            PrepararFormulario(reserva, mostrarPagoInicial: true);
            return View(reserva);
        }

        TempData["Mensaje"] =
            "La reserva y su pago inicial se registraron correctamente.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Edit(int id)
    {
        var reserva = repositorio.ObtenerPorId(id);
        if (reserva is null)
        {
            return NotFound();
        }

        if (reserva.FechaTerminacionAnticipada.HasValue)
        {
            TempData["Error"] = "Una reserva terminada anticipadamente no puede modificarse.";
            return RedirectToAction(nameof(Details), new { id });
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

        if (reservaActual.FechaTerminacionAnticipada.HasValue)
        {
            TempData["Error"] = "Una reserva terminada anticipadamente no puede modificarse.";
            return RedirectToAction(nameof(Details), new { id });
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

    public IActionResult Renovar(int id)
    {
        var reserva = repositorio.ObtenerPorId(id);
        if (reserva is null)
        {
            return NotFound();
        }

        if (reserva.Inmueble is null || !reserva.Inmueble.Disponible)
        {
            TempData["Error"] =
                "No se puede renovar porque la oferta del inmueble está suspendida.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var finEfectivo = reserva.FechaTerminacionAnticipada?.Date
            ?? reserva.FechaHasta.Date;
        return View(new RenovacionReservaViewModel
        {
            IdReservaOrigen = reserva.IdReserva,
            FechaDesde = finEfectivo.AddDays(1),
            FechaHasta = finEfectivo.AddDays(2),
            MontoDia = reserva.Inmueble.PrecioDia,
            ReservaOrigen = reserva
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Renovar(RenovacionReservaViewModel modelo)
    {
        var reservaOrigen = repositorio.ObtenerPorId(modelo.IdReservaOrigen);
        if (reservaOrigen is null)
        {
            return NotFound();
        }

        modelo.ReservaOrigen = reservaOrigen;
        var finEfectivo = reservaOrigen.FechaTerminacionAnticipada?.Date
            ?? reservaOrigen.FechaHasta.Date;
        if (modelo.FechaDesde.Date <= finEfectivo)
        {
            ModelState.AddModelError(
                nameof(RenovacionReservaViewModel.FechaDesde),
                $"La renovación debe comenzar después del {finEfectivo:dd/MM/yyyy}.");
        }

        if (reservaOrigen.Inmueble is null || !reservaOrigen.Inmueble.Disponible)
        {
            ModelState.AddModelError(
                string.Empty,
                "No se puede renovar porque la oferta del inmueble está suspendida.");
        }

        if (modelo.FechaHasta.Date > modelo.FechaDesde.Date
            && repositorio.ExisteSuperposicion(
                reservaOrigen.IdInmueble,
                modelo.FechaDesde,
                modelo.FechaHasta))
        {
            ModelState.AddModelError(
                nameof(RenovacionReservaViewModel.FechaHasta),
                "El inmueble ya posee otra reserva que se superpone con esas fechas.");
        }

        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        var idUsuario = ObtenerIdUsuarioActual();
        if (!idUsuario.HasValue)
        {
            return Unauthorized();
        }

        var nuevaReserva = new Reserva
        {
            IdInmueble = reservaOrigen.IdInmueble,
            IdInquilino = reservaOrigen.IdInquilino,
            FechaDesde = modelo.FechaDesde.Date,
            FechaHasta = modelo.FechaHasta.Date,
            MontoDia = modelo.MontoDia
        };

        try
        {
            repositorio.Alta(
                nuevaReserva,
                idUsuario.Value,
                reservaOrigen.Inmueble!.PorcentajeReserva,
                reservaOrigen.IdReserva);
        }
        catch (MySqlException exception) when (exception.Number == 1452)
        {
            logger.LogError(
                exception,
                "Error de relación al renovar la reserva {IdReserva}.",
                reservaOrigen.IdReserva);
            ModelState.AddModelError(
                string.Empty,
                "No se pudo renovar porque cambió un dato relacionado.");
            return View(modelo);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al renovar la reserva {IdReserva}.",
                reservaOrigen.IdReserva);
            ModelState.AddModelError(
                string.Empty,
                "No se pudo crear la renovación. Intente nuevamente.");
            return View(modelo);
        }

        TempData["Mensaje"] =
            $"La renovación Nº {nuevaReserva.IdReserva} y su pago inicial se registraron correctamente.";
        return RedirectToAction(nameof(Details), new { id = nuevaReserva.IdReserva });
    }

    public IActionResult Terminacion(int id, DateTime? fechaTerminacion = null)
    {
        var reserva = repositorio.ObtenerPorId(id);
        if (reserva is null)
        {
            return NotFound();
        }

        var fechaMinima = ObtenerFechaMinimaTerminacion(reserva);
        var fechaMaxima = reserva.FechaHasta.Date.AddDays(-1);
        if (reserva.FechaTerminacionAnticipada.HasValue)
        {
            TempData["Error"] = "La reserva ya posee una terminación anticipada.";
            return RedirectToAction(nameof(Details), new { id });
        }

        if (fechaMinima > fechaMaxima)
        {
            TempData["Error"] = "La reserva ya finalizó y no puede terminarse anticipadamente.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var fechaSeleccionada = fechaTerminacion?.Date ?? fechaMinima;
        if (fechaSeleccionada < fechaMinima || fechaSeleccionada > fechaMaxima)
        {
            TempData["Error"] =
                $"La fecha debe estar comprendida entre {fechaMinima:dd/MM/yyyy} y {fechaMaxima:dd/MM/yyyy}.";
            return RedirectToAction(nameof(Terminacion), new { id });
        }

        ViewBag.FechaMinima = fechaMinima.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        ViewBag.FechaMaxima = fechaMaxima.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        return View(CalcularTerminacion(reserva, fechaSeleccionada));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Terminacion(TerminacionReservaViewModel modelo)
    {
        var reserva = repositorio.ObtenerPorId(modelo.IdReserva);
        if (reserva is null)
        {
            return NotFound();
        }

        var fechaMinima = ObtenerFechaMinimaTerminacion(reserva);
        var fechaMaxima = reserva.FechaHasta.Date.AddDays(-1);
        if (reserva.FechaTerminacionAnticipada.HasValue)
        {
            TempData["Error"] = "La reserva ya posee una terminación anticipada.";
            return RedirectToAction(nameof(Details), new { id = reserva.IdReserva });
        }

        if (fechaMinima > fechaMaxima
            || modelo.FechaTerminacion.Date < fechaMinima
            || modelo.FechaTerminacion.Date > fechaMaxima)
        {
            TempData["Error"] = "La fecha indicada no es válida para esta reserva.";
            return RedirectToAction(nameof(Terminacion), new { id = reserva.IdReserva });
        }

        var idUsuario = ObtenerIdUsuarioActual();
        if (!idUsuario.HasValue)
        {
            return Unauthorized();
        }

        var calculo = CalcularTerminacion(reserva, modelo.FechaTerminacion.Date);
        try
        {
            if (!repositorio.TerminarAnticipadamente(
                reserva.IdReserva,
                calculo.FechaTerminacion,
                calculo.MontoMulta,
                idUsuario.Value))
            {
                TempData["Error"] = "La reserva ya había sido terminada por otro usuario.";
                return RedirectToAction(nameof(Details), new { id = reserva.IdReserva });
            }
        }
        catch (MySqlException exception) when (exception.Number == 1452)
        {
            logger.LogError(
                exception,
                "Error de relación al terminar la reserva {IdReserva}.",
                reserva.IdReserva);
            TempData["Error"] =
                "No se pudo registrar la terminación porque cambió un dato relacionado.";
            return RedirectToAction(nameof(Terminacion), new { id = reserva.IdReserva });
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al terminar anticipadamente la reserva {IdReserva}.",
                reserva.IdReserva);
            TempData["Error"] =
                "No se pudo completar la terminación anticipada. Intente nuevamente.";
            return RedirectToAction(nameof(Terminacion), new { id = reserva.IdReserva });
        }

        TempData["Mensaje"] =
            "La reserva se terminó anticipadamente y la multa quedó registrada como pago.";
        return RedirectToAction(nameof(Details), new { id = reserva.IdReserva });
    }

    [Authorize(Roles = RolesUsuario.Administrador)]
    public IActionResult Delete(int id)
    {
        var reserva = repositorio.ObtenerPorId(id);
        return reserva is null ? NotFound() : View(reserva);
    }

    [Authorize(Roles = RolesUsuario.Administrador)]
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
                precioDia = i.PrecioDia,
                porcentajeReserva = i.PorcentajeReserva
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

    private void PrepararFormulario(
        Reserva? reserva = null,
        bool mostrarPagoInicial = false)
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
        ViewBag.MostrarPagoInicial = mostrarPagoInicial;
        ViewBag.PorcentajeReservaSeleccionado = inmueble?.PorcentajeReserva ?? 0m;
    }

    private static string? NormalizarEstado(string? estado) =>
        !string.IsNullOrWhiteSpace(estado) && EstadosPermitidos.Contains(estado)
            ? estado.ToLowerInvariant()
            : null;

    private static DateTime ObtenerFechaMinimaTerminacion(Reserva reserva) =>
        DateTime.Today > reserva.FechaDesde.Date
            ? DateTime.Today
            : reserva.FechaDesde.Date;

    private static TerminacionReservaViewModel CalcularTerminacion(
        Reserva reserva,
        DateTime fechaTerminacion)
    {
        var diasOriginales = (reserva.FechaHasta.Date - reserva.FechaDesde.Date).Days;
        var diasCumplidos = Math.Max(
            0,
            (fechaTerminacion.Date - reserva.FechaDesde.Date).Days);
        var diasRestantes = (reserva.FechaHasta.Date - fechaTerminacion.Date).Days;
        var porcentaje = diasCumplidos < diasOriginales / 2m ? 0.50m : 0.25m;
        var montoMulta = decimal.Round(
            diasRestantes * reserva.MontoDia * porcentaje,
            2,
            MidpointRounding.AwayFromZero);

        return new TerminacionReservaViewModel
        {
            IdReserva = reserva.IdReserva,
            FechaTerminacion = fechaTerminacion.Date,
            Reserva = reserva,
            DiasOriginales = diasOriginales,
            DiasCumplidos = diasCumplidos,
            DiasRestantes = diasRestantes,
            PorcentajeMulta = porcentaje,
            MontoMulta = montoMulta
        };
    }

    private int? ObtenerIdUsuarioActual()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;
    }
}
