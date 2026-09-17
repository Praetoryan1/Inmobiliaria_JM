using System.Globalization;
using System.Security.Claims;
using inmobiliaria.Models;
using inmobiliaria.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Controllers;

public class PagosController : Controller
{
    private const int TamPagina = 10;
    private readonly RepositorioPagos repositorio;
    private readonly RepositorioReservas repositorioReservas;
    private readonly ILogger<PagosController> logger;

    public PagosController(
        RepositorioPagos repositorio,
        RepositorioReservas repositorioReservas,
        ILogger<PagosController> logger)
    {
        this.repositorio = repositorio;
        this.repositorioReservas = repositorioReservas;
        this.logger = logger;
    }

    public IActionResult Index(
        int idReserva,
        string? busqueda = null,
        int pagina = 1)
    {
        var modelo = ConstruirListado(idReserva, busqueda, pagina);
        return modelo is null ? NotFound() : View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(PagoCrearViewModel modelo)
    {
        modelo.Concepto = (modelo.Concepto ?? string.Empty).Trim();
        ValidarFechaPago(modelo);

        var reserva = repositorioReservas.ObtenerPorId(modelo.IdReserva);
        if (reserva is null)
        {
            ModelState.AddModelError(
                nameof(PagoCrearViewModel.IdReserva),
                "La reserva seleccionada no existe.");
        }

        var idUsuario = ObtenerIdUsuarioActual();
        if (!idUsuario.HasValue)
        {
            return Unauthorized();
        }

        if (!ModelState.IsValid)
        {
            var listado = ConstruirListado(
                modelo.IdReserva,
                null,
                1,
                modelo);
            return listado is null ? NotFound() : View(nameof(Index), listado);
        }

        var pago = new Pago
        {
            IdReserva = modelo.IdReserva,
            Concepto = modelo.Concepto,
            FechaPago = modelo.FechaPago,
            Importe = modelo.Importe,
            IdUsuarioCreador = idUsuario.Value
        };

        try
        {
            repositorio.Alta(pago);
        }
        catch (MySqlException exception) when (exception.Number == 1452)
        {
            ModelState.AddModelError(
                string.Empty,
                "La reserva o el usuario asociado ya no existe.");
            var listado = ConstruirListado(modelo.IdReserva, null, 1, modelo);
            return listado is null ? NotFound() : View(nameof(Index), listado);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error al registrar un pago para la reserva {IdReserva}.",
                modelo.IdReserva);
            ModelState.AddModelError(
                string.Empty,
                "No se pudo registrar el pago. Intente nuevamente.");
            var listado = ConstruirListado(modelo.IdReserva, null, 1, modelo);
            return listado is null ? NotFound() : View(nameof(Index), listado);
        }

        TempData["Mensaje"] = "El pago se registró correctamente.";
        return RedirectToAction(nameof(Index), new { idReserva = modelo.IdReserva });
    }

    public IActionResult Details(int id)
    {
        var pago = repositorio.ObtenerPorId(id);
        if (pago is null)
        {
            return NotFound();
        }

        pago.Reserva = repositorioReservas.ObtenerPorId(pago.IdReserva);
        return View(pago);
    }

    public IActionResult Edit(int id)
    {
        var pago = repositorio.ObtenerPorId(id);
        if (pago is null)
        {
            return NotFound();
        }

        if (pago.Anulado)
        {
            TempData["Error"] = "Un pago anulado no puede modificarse.";
            return RedirectToAction(nameof(Index), new { idReserva = pago.IdReserva });
        }

        ViewBag.Pago = pago;
        return View(new PagoEditarViewModel
        {
            IdPago = pago.IdPago,
            IdReserva = pago.IdReserva,
            Concepto = pago.Concepto
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Edit(int id, PagoEditarViewModel modelo)
    {
        if (id != modelo.IdPago)
        {
            return NotFound();
        }

        var pago = repositorio.ObtenerPorId(id);
        if (pago is null || pago.IdReserva != modelo.IdReserva)
        {
            return NotFound();
        }

        modelo.Concepto = (modelo.Concepto ?? string.Empty).Trim();
        if (pago.Anulado)
        {
            ModelState.AddModelError(
                string.Empty,
                "Un pago anulado no puede modificarse.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Pago = pago;
            return View(modelo);
        }

        try
        {
            repositorio.ModificarConcepto(id, modelo.Concepto);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al modificar el pago {IdPago}.", id);
            ModelState.AddModelError(
                string.Empty,
                "No se pudo modificar el pago. Intente nuevamente.");
            ViewBag.Pago = pago;
            return View(modelo);
        }

        TempData["Mensaje"] = "El concepto del pago se actualizó correctamente.";
        return RedirectToAction(nameof(Index), new { idReserva = pago.IdReserva });
    }

    [Authorize(Roles = RolesUsuario.Administrador)]
    public IActionResult Anular(int id)
    {
        var pago = repositorio.ObtenerPorId(id);
        if (pago is null)
        {
            return NotFound();
        }

        if (pago.Anulado)
        {
            TempData["Error"] = "El pago ya se encuentra anulado.";
            return RedirectToAction(nameof(Index), new { idReserva = pago.IdReserva });
        }

        pago.Reserva = repositorioReservas.ObtenerPorId(pago.IdReserva);
        return View(pago);
    }

    [Authorize(Roles = RolesUsuario.Administrador)]
    [HttpPost, ActionName("Anular")]
    [ValidateAntiForgeryToken]
    public IActionResult AnularConfirmed(int id)
    {
        var pago = repositorio.ObtenerPorId(id);
        if (pago is null)
        {
            return NotFound();
        }

        if (pago.Anulado)
        {
            TempData["Error"] = "El pago ya se encuentra anulado.";
            return RedirectToAction(nameof(Index), new { idReserva = pago.IdReserva });
        }

        var idUsuario = ObtenerIdUsuarioActual();
        if (!idUsuario.HasValue)
        {
            return Unauthorized();
        }

        try
        {
            if (!repositorio.Anular(id, idUsuario.Value))
            {
                TempData["Error"] = "El pago ya se encontraba anulado.";
                return RedirectToAction(nameof(Index), new { idReserva = pago.IdReserva });
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al anular el pago {IdPago}.", id);
            TempData["Error"] = "No se pudo anular el pago. Intente nuevamente.";
            return RedirectToAction(nameof(Index), new { idReserva = pago.IdReserva });
        }

        TempData["Mensaje"] = "El pago se anuló correctamente y se conservó en el historial.";
        return RedirectToAction(nameof(Index), new { idReserva = pago.IdReserva });
    }

    private PagoListadoViewModel? ConstruirListado(
        int idReserva,
        string? busqueda,
        int pagina,
        PagoCrearViewModel? nuevoPago = null)
    {
        var reserva = repositorioReservas.ObtenerPorId(idReserva);
        if (reserva is null)
        {
            return null;
        }

        var cantidadTotal = repositorio.ObtenerCantidadPorReserva(idReserva, busqueda);
        var totalPaginas = Math.Max(
            1,
            (int)Math.Ceiling(cantidadTotal / (double)TamPagina));
        pagina = Math.Clamp(pagina, 1, totalPaginas);

        return new PagoListadoViewModel
        {
            Reserva = reserva,
            Pagos = repositorio.ObtenerListaPorReserva(
                idReserva,
                busqueda,
                pagina,
                TamPagina),
            NuevoPago = nuevoPago ?? new PagoCrearViewModel
            {
                IdReserva = idReserva,
                FechaPago = DateTime.Today
            },
            Busqueda = busqueda?.Trim() ?? string.Empty,
            PaginaActual = pagina,
            TotalPaginas = totalPaginas,
            CantidadTotal = cantidadTotal,
            TotalRegistrado = repositorio.ObtenerTotalRegistrado(idReserva)
        };
    }

    private void ValidarFechaPago(PagoCrearViewModel modelo)
    {
        if (modelo.FechaPago == default)
        {
            ModelState.AddModelError(
                nameof(PagoCrearViewModel.FechaPago),
                "La fecha de pago es obligatoria.");
        }
        else if (modelo.FechaPago.Date > DateTime.Today)
        {
            ModelState.AddModelError(
                nameof(PagoCrearViewModel.FechaPago),
                "La fecha de pago no puede ser futura.");
        }
    }

    private int? ObtenerIdUsuarioActual()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? id
            : null;
    }
}
