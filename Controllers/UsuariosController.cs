using System.Globalization;
using System.Security.Claims;
using inmobiliaria.Models;
using inmobiliaria.Repositories;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;

namespace inmobiliaria.Controllers;

public class UsuariosController : Controller
{
    private const long TamanoMaximoAvatar = 5 * 1024 * 1024;
    private static readonly HashSet<string> ExtensionesAvatarPermitidas =
        new(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly RepositorioUsuarios repositorio;
    private readonly IPasswordHasher<Usuario> passwordHasher;
    private readonly IWebHostEnvironment environment;
    private readonly ILogger<UsuariosController> logger;

    public UsuariosController(
        RepositorioUsuarios repositorio,
        IPasswordHasher<Usuario> passwordHasher,
        IWebHostEnvironment environment,
        ILogger<UsuariosController> logger)
    {
        this.repositorio = repositorio;
        this.passwordHasher = passwordHasher;
        this.environment = environment;
        this.logger = logger;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewBag.ReturnUrl = returnUrl;
        return View(new LoginViewModel());
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(
        LoginViewModel modelo,
        string? returnUrl = null)
    {
        ViewBag.ReturnUrl = returnUrl;
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        Usuario? usuario;
        try
        {
            usuario = repositorio.ObtenerPorEmail(modelo.Email);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error al consultar el usuario durante el ingreso.");
            ModelState.AddModelError(
                string.Empty,
                "No se pudo iniciar sesión. Intente nuevamente.");
            return View(modelo);
        }

        if (usuario is null)
        {
            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            return View(modelo);
        }

        var resultado = passwordHasher.VerifyHashedPassword(
            usuario,
            usuario.PasswordHash,
            modelo.Password);

        if (resultado == PasswordVerificationResult.Failed)
        {
            ModelState.AddModelError(string.Empty, "Email o contraseña incorrectos.");
            return View(modelo);
        }

        if (resultado == PasswordVerificationResult.SuccessRehashNeeded)
        {
            repositorio.ActualizarPasswordHash(
                usuario.IdUsuario,
                passwordHasher.HashPassword(usuario, modelo.Password));
        }

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CrearPrincipal(usuario),
            new AuthenticationProperties
            {
                IsPersistent = modelo.Recordarme,
                ExpiresUtc = modelo.Recordarme
                    ? DateTimeOffset.UtcNow.AddDays(14)
                    : null
            });

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    public IActionResult Perfil()
    {
        var usuario = ObtenerUsuarioActual();
        if (usuario is null)
        {
            return NotFound();
        }

        ViewBag.Rol = usuario.Rol;
        return View(new PerfilUsuarioViewModel
        {
            Nombre = usuario.Nombre,
            Apellido = usuario.Apellido,
            Email = usuario.Email,
            AvatarActual = usuario.Avatar
        });
    }

    [Authorize]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Perfil(PerfilUsuarioViewModel modelo)
    {
        var usuario = ObtenerUsuarioActual();
        if (usuario is null)
        {
            return NotFound();
        }

        modelo.Nombre = modelo.Nombre.Trim();
        modelo.Apellido = modelo.Apellido.Trim();
        modelo.Email = modelo.Email.Trim().ToLowerInvariant();
        modelo.AvatarActual = usuario.Avatar;
        ViewBag.Rol = usuario.Rol;

        if (repositorio.ExisteEmail(modelo.Email, usuario.IdUsuario))
        {
            ModelState.AddModelError(
                nameof(PerfilUsuarioViewModel.Email),
                "Ya existe otro usuario registrado con ese email.");
        }

        var cambiaPassword = !string.IsNullOrWhiteSpace(modelo.PasswordActual)
            || !string.IsNullOrWhiteSpace(modelo.PasswordNueva)
            || !string.IsNullOrWhiteSpace(modelo.ConfirmarPasswordNueva);

        string? nuevoPasswordHash = null;
        if (cambiaPassword)
        {
            if (string.IsNullOrWhiteSpace(modelo.PasswordActual))
            {
                ModelState.AddModelError(
                    nameof(PerfilUsuarioViewModel.PasswordActual),
                    "Debe ingresar su contraseña actual.");
            }

            if (string.IsNullOrWhiteSpace(modelo.PasswordNueva))
            {
                ModelState.AddModelError(
                    nameof(PerfilUsuarioViewModel.PasswordNueva),
                    "Debe ingresar la nueva contraseña.");
            }

            if (!string.IsNullOrWhiteSpace(modelo.PasswordActual)
                && passwordHasher.VerifyHashedPassword(
                    usuario,
                    usuario.PasswordHash,
                    modelo.PasswordActual) == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(
                    nameof(PerfilUsuarioViewModel.PasswordActual),
                    "La contraseña actual es incorrecta.");
            }

            if (ModelState.IsValid)
            {
                nuevoPasswordHash = passwordHasher.HashPassword(
                    usuario,
                    modelo.PasswordNueva!);
            }
        }

        ValidarAvatar(modelo.AvatarArchivo);
        if (!ModelState.IsValid)
        {
            return View(modelo);
        }

        string? nuevaRutaAvatar = null;
        try
        {
            if (modelo.AvatarArchivo is not null)
            {
                nuevaRutaAvatar = await GuardarAvatar(modelo.AvatarArchivo);
            }

            usuario.Nombre = modelo.Nombre;
            usuario.Apellido = modelo.Apellido;
            usuario.Email = modelo.Email;
            usuario.Avatar = nuevaRutaAvatar ?? usuario.Avatar;

            if (!repositorio.ActualizarPerfil(usuario, nuevoPasswordHash))
            {
                EliminarAvatarNuevo(nuevaRutaAvatar);
                return NotFound();
            }
        }
        catch (MySqlException exception) when (exception.Number == 1062)
        {
            EliminarAvatarNuevo(nuevaRutaAvatar);
            ModelState.AddModelError(
                nameof(PerfilUsuarioViewModel.Email),
                "Ya existe otro usuario registrado con ese email.");
            return View(modelo);
        }
        catch (Exception exception)
        {
            EliminarAvatarNuevo(nuevaRutaAvatar);
            logger.LogError(
                exception,
                "Error al actualizar el perfil del usuario {IdUsuario}.",
                usuario.IdUsuario);
            ModelState.AddModelError(
                string.Empty,
                "No se pudo actualizar el perfil. Intente nuevamente.");
            return View(modelo);
        }

        var autenticacion = await HttpContext.AuthenticateAsync(
            CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            CrearPrincipal(usuario),
            autenticacion.Properties ?? new AuthenticationProperties());

        TempData["Mensaje"] = "El perfil se actualizó correctamente.";
        return RedirectToAction(nameof(Perfil));
    }

    [AllowAnonymous]
    public IActionResult AccesoDenegado() => View();

    private Usuario? ObtenerUsuarioActual()
    {
        var valor = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(valor, NumberStyles.None, CultureInfo.InvariantCulture, out var id)
            ? repositorio.ObtenerPorId(id)
            : null;
    }

    private void ValidarAvatar(IFormFile? archivo)
    {
        if (archivo is null)
        {
            return;
        }

        var extension = Path.GetExtension(archivo.FileName);
        if (!ExtensionesAvatarPermitidas.Contains(extension))
        {
            ModelState.AddModelError(
                nameof(PerfilUsuarioViewModel.AvatarArchivo),
                "El avatar debe ser una imagen JPG, PNG o WEBP.");
        }

        if (archivo.Length == 0 || archivo.Length > TamanoMaximoAvatar)
        {
            ModelState.AddModelError(
                nameof(PerfilUsuarioViewModel.AvatarArchivo),
                "El avatar debe pesar entre 1 byte y 5 MB.");
        }
    }

    private async Task<string> GuardarAvatar(IFormFile archivo)
    {
        var extension = Path.GetExtension(archivo.FileName).ToLowerInvariant();
        var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
        var directorio = Path.Combine(environment.WebRootPath, "uploads", "avatars");
        Directory.CreateDirectory(directorio);

        var rutaFisica = Path.Combine(directorio, nombreArchivo);
        await using var flujo = System.IO.File.Create(rutaFisica);
        await archivo.CopyToAsync(flujo);

        return $"/uploads/avatars/{nombreArchivo}";
    }

    private void EliminarAvatarNuevo(string? rutaRelativa)
    {
        if (string.IsNullOrWhiteSpace(rutaRelativa)
            || !rutaRelativa.StartsWith("/uploads/avatars/", StringComparison.Ordinal))
        {
            return;
        }

        var raizAvatares = Path.GetFullPath(
            Path.Combine(environment.WebRootPath, "uploads", "avatars"));
        var rutaFisica = Path.GetFullPath(
            Path.Combine(environment.WebRootPath, rutaRelativa.TrimStart('/')));

        if (rutaFisica.StartsWith(raizAvatares, StringComparison.OrdinalIgnoreCase)
            && System.IO.File.Exists(rutaFisica))
        {
            System.IO.File.Delete(rutaFisica);
        }
    }

    private static ClaimsPrincipal CrearPrincipal(Usuario usuario)
    {
        var identidad = new ClaimsIdentity(
            new[]
            {
                new Claim(
                    ClaimTypes.NameIdentifier,
                    usuario.IdUsuario.ToString(CultureInfo.InvariantCulture)),
                new Claim(ClaimTypes.Name, usuario.NombreCompleto),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, usuario.Rol)
            },
            CookieAuthenticationDefaults.AuthenticationScheme);

        return new ClaimsPrincipal(identidad);
    }
}
