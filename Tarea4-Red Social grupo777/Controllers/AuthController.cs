
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

    [HttpGet("GithubLogin")]
    public async Task<IActionResult> GithubLogin()
    {
        var clientId = _configuration["GitHub:ClientId"];
        var redirectUri = "http://localhost:5290/api/auth/callback";

        var url = "https://github.com/login/oauth/authorize" +
        $"?client_id={clientId}" +
        $"&redirect_uri={redirectUri}" +
        $"&scope=user";
        return Redirect(url);
    }
    [HttpGet("callback")]
    public async Task<IActionResult> Callback2([FromQuery] string code)
    {
        var clientId = _configuration["GitHub:ClientId"];
        var clientSecret = _configuration["GitHub:ClientSecret"];

        var client = _httpClientFactory.CreateClient();

        var response = await client.PostAsync(
        "https://github.com/login/oauth/access_token",
        new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["code"] = code
        })
        );

        var body = await response.Content.ReadAsStringAsync();

        var token = body
        .Split('&')
        .First(x => x.StartsWith("access_token="))
        .Replace("access_token=", "");

        HttpContext.Session.SetString("GitHubToken", token);
        return Redirect("http://localhost:5500/Tarea4-Red%20Social%20grupo777/Frontend/Bienvenida/bienvenida.html");
    }
    [HttpGet("EstaAutenticado")]
    public async Task<IActionResult> VerificarToken()
    {
        var token = HttpContext.Session.GetString("GitHubToken");

        if (token == null)
            return Unauthorized("No hay token guardado");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("Authorization", $"Bearer {token}");
        client.DefaultRequestHeaders.Add("User-Agent", "OAuth-Demo");

        var response = await client.GetAsync("https://api.github.com/user");

        if (!response.IsSuccessStatusCode)
        {
            // Token inválido o revocado
            HttpContext.Session.Remove("GitHubToken");
            return Unauthorized("Token inválido o revocado");
        }

        var userJson = await response.Content.ReadAsStringAsync();
        return Ok(userJson);
    }
}
