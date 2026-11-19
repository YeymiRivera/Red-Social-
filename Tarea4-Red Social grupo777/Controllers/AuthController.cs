
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Context _context;

    public AuthController(Context context)
    {
        _context = context;
    }

    // Iniciar sesión con Google
    [HttpGet("GoogleLogin")]
    public IActionResult GoogleLogin()
    {
        var propiedades = new AuthenticationProperties
        {
            RedirectUri = Url.Action(
                nameof(GoogleResponse),
                "Auth"
            )
        };

        return Challenge(
            propiedades,
            GoogleDefaults.AuthenticationScheme
        );
    }

    // Recibir los datos de Google
    [HttpGet("GoogleResponse")]
    public async Task<IActionResult> GoogleResponse()
    {
        var resultado = await HttpContext.AuthenticateAsync("External");

        if (!resultado.Succeeded || resultado.Principal == null)
        {
            return Unauthorized(new
            {
                mensaje = "No se pudo autenticar con Google."
            });
        }

        var identidad = resultado.Principal;

        var googleId = identidad.FindFirst(
            ClaimTypes.NameIdentifier)?.Value;

        var email = identidad.FindFirst(
            ClaimTypes.Email)?.Value;

        var nombre = identidad.FindFirst(
            ClaimTypes.Name)?.Value;

        if (string.IsNullOrEmpty(googleId) ||
            string.IsNullOrEmpty(email))
        {
            return Unauthorized(new
            {
                mensaje = "Google no proporcionó los datos necesarios."
            });
        }

        // Buscar usuario en PostgreSQL
        var consulta =
            from u in _context.Usuarios
            where u.GoogleId == googleId
            select u;

        var usuario = await consulta.FirstOrDefaultAsync();

        // Registrar si todavía no existe
        if (usuario == null)
        {
            usuario = new Usuario
            {
                User = nombre ?? email.Split('@')[0],
                email = email,
                Biografia = "",
                Fecha = DateTime.UtcNow.ToString("O"),
                Estado = "Activo",
                GoogleId = googleId
            };

            _context.Usuarios.Add(usuario);

            await _context.SaveChangesAsync();
        }

        // Cerrar la cookie temporal
        await HttpContext.SignOutAsync("External");
        return Redirect("http://localhost:5500/Tarea4-Red%20Social%20grupo777/Frontend/Bienvenida/bienvenida.html");
    }
}
