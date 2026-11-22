
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Context _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(Context context, IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _context = context;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
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
        ;

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
        ;

        // Buscar usuario en PostgreSQL
        var consulta = await (
            from u in _context.CuentasExternas
            where u.ProveedorId == googleId
            select u).FirstOrDefaultAsync();
        if (consulta == null)
        {
            Usuario usuario = new Usuario
            {
                User = nombre ?? email.Split('@')[0],
                email = email,
                Biografia = "",
                Fecha = DateTime.UtcNow.ToString("O"),
                Estado = "Activo",
            };
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
            var cuentaExterna = new CuentaExterna
            {
                ProveedorId = googleId,
                Proveedor = "Google",
                UsuarioId = usuario.Id
            };
            _context.CuentasExternas.Add(cuentaExterna);
            await _context.SaveChangesAsync();
        }
        ;
        // Cerrar la cookie temporal
        await HttpContext.SignOutAsync("External");
        return Redirect("http://localhost:5500/Tarea4-Red%20Social%20grupo777/Frontend/Bienvenida/bienvenida.html");
    }

    [HttpGet("GithubLogin")]
    public async Task<IActionResult> GithubLogin()
    {
        var clientId = _configuration["GitHub:ClientId"];
        var redirectUri = "http://localhost:5290/api/auth/callback";

        var url = "https://github.com/login/oauth/authorize" +
        $"?client_id={clientId}" +
        $"&redirect_uri={redirectUri}" +
        $"&scope=read:user%20user:email";
        return Redirect(url);
    }
    [HttpGet("callback")]
    public async Task<IActionResult> Callback2([FromQuery] string code)
    {
        var clientId = _configuration["GitHub:ClientId"];
        var clientSecret = _configuration["GitHub:ClientSecret"];

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(
    new MediaTypeWithQualityHeaderValue("application/json")
    );

        // 1. Solicitar el access token a GitHub
        var respuestaToken = await client.PostAsync(
            "https://github.com/login/oauth/access_token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId!,
                ["client_secret"] = clientSecret!,
                ["code"] = code
            })
        );

        if (!respuestaToken.IsSuccessStatusCode)
            return Unauthorized("No se pudo obtener el token de GitHub.");

        var contenidoToken = await respuestaToken.Content.ReadAsStringAsync();
        Console.WriteLine(contenidoToken);

        var documentoToken = JsonDocument.Parse(contenidoToken);

        var token = documentoToken.RootElement
            .GetProperty("access_token").GetString();

        if (string.IsNullOrEmpty(token))
            return Unauthorized("GitHub no proporcionó un token.");

        // 2. Consultar información del usuario en GitHub
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        client.DefaultRequestHeaders.UserAgent.ParseAdd("RedSocialApp");

        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json")
        );

        var respuestaUsuario = await client.GetAsync(
            "https://api.github.com/user"
        );

        if (!respuestaUsuario.IsSuccessStatusCode)
            return Unauthorized("No se pudo obtener el perfil de GitHub.");

        var contenidoUsuario = await respuestaUsuario.Content.ReadAsStringAsync();

        var documento = JsonDocument.Parse(contenidoUsuario);

        var root = documento.RootElement;
        // 3. Extraer información del perfil
        var githubId = root.GetProperty("id").GetInt64().ToString();

        var username = root.GetProperty("login").GetString();

        var nombre = root.TryGetProperty("name", out var nombreJson)
            ? nombreJson.GetString()
            : null;

        var email = root.TryGetProperty("email", out var emailJson)
            ? emailJson.GetString()
            : null;

        var biografia = root.TryGetProperty("bio", out var bioJson)
            ? bioJson.GetString()
            : null;

        // 4. Buscar la cuenta externa
        var consulta = await (
            from c in _context.CuentasExternas
            where c.ProveedorId == githubId
               && c.Proveedor == "GitHub"
            select c
        ).FirstOrDefaultAsync();

        Usuario usuario;

        if (consulta == null)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return BadRequest(
                    "GitHub no proporcionó un correo. Verifica los permisos user:email."
                );
            }
            // 5. Crear usuario local
            usuario = new Usuario
            {
                User = username ?? "UsuarioGitHub",
                email = email,
                Biografia = biografia ?? "",
                Fecha = DateTime.UtcNow.ToString("O"),
                Estado = "Activo"
            };

            _context.Usuarios.Add(usuario);

            await _context.SaveChangesAsync();

            // 6. Crear relación con GitHub
            var cuentaExterna = new CuentaExterna
            {
                UsuarioId = usuario.Id,
                Proveedor = "GitHub",
                ProveedorId = githubId
            };
            _context.CuentasExternas.Add(cuentaExterna);

            await _context.SaveChangesAsync();
        }
        ;
        return Redirect(
            "http://localhost:5500/Tarea4-Red%20Social%20grupo777/Frontend/Bienvenida/bienvenida.html"
        );
    }
}
