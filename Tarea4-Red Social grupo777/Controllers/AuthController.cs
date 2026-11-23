
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Text;
using Microsoft.AspNetCore.DataProtection;

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
     from c in _context.CuentasExternas
     where c.ProveedorId == googleId
        && c.Proveedor == "Google"
     select c
 ).FirstOrDefaultAsync();

        Usuario usuario;

        if (consulta == null)
        {
            usuario = new Usuario
            {
                User = nombre ?? email.Split('@')[0],
                email = email,
                Biografia = "",
                Fecha = DateTime.UtcNow.ToString("O"),
                Estado = "Activo"
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
        else
        {
            usuario = await _context.Usuarios.FindAsync(consulta.UsuarioId);

            if (usuario == null)
            {
                return Unauthorized("No se encontró el usuario local.");
            }
        }

        // Generar la cookie de autenticación
        var claims = new List<Claim>
        {
        new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
        new Claim(ClaimTypes.Name, usuario.User),
        new Claim(ClaimTypes.Email, usuario.email)
        };

        var identidadclaim = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identidadclaim);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal
        );
        Console.WriteLine("Usuario autenticado: " + User.Identity?.IsAuthenticated);
        // Cerrar la cookie temporal
        await HttpContext.SignOutAsync("External");
        var frontendUrl = _configuration["FrontendUrl"];
        return Redirect($"{frontendUrl}/Bienvenida/bienvenida.html");
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
        else
        {
            usuario = await _context.Usuarios.FindAsync(consulta.UsuarioId);

            if (usuario == null)
                return Unauthorized("No se encontró el usuario local.");
        }
        var claims = new List<Claim>
            {
            new Claim(
                ClaimTypes.NameIdentifier,
                usuario.Id.ToString()
            ),
            new Claim(
                ClaimTypes.Name,
                usuario.User
            ),
            new Claim(
                ClaimTypes.Email,
                usuario.email)
            };

        var identidadclaim = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme
        );

        var principal = new ClaimsPrincipal(identidadclaim);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            principal
        );
        var frontendUrl = _configuration["FrontendUrl"];
        return Redirect($"{frontendUrl}/Bienvenida/bienvenida.html");
    }
     [HttpGet("dropbox/login")]
    public IActionResult DropboxLogin()
    {
        var clientId = Environment.GetEnvironmentVariable("DROPBOX_CLIENT_ID");
        var redirectUri = Uri.EscapeDataString(Environment.GetEnvironmentVariable("DROPBOX_REDIRECT_URI") ?? "");
        var url = $"https://www.dropbox.com/oauth2/authorize?client_id={clientId}&redirect_uri={redirectUri}&response_type=code";
        return Redirect(url);
    }

    [HttpGet("dropbox/callback")]
    public async Task<IActionResult> DropboxCallback([FromQuery] string? code)
    {
        if (string.IsNullOrEmpty(code)) return BadRequest("No se recibió el código de Dropbox.");

        var client = _httpClientFactory.CreateClient();
        var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = Environment.GetEnvironmentVariable("DROPBOX_CLIENT_ID") ?? "",
            ["client_secret"] = Environment.GetEnvironmentVariable("DROPBOX_CLIENT_SECRET") ?? "",
            ["grant_type"] = "authorization_code",
            ["code"] = code,
            ["redirect_uri"] = Environment.GetEnvironmentVariable("DROPBOX_REDIRECT_URI") ?? ""
        });

        var tokenRes = await client.PostAsync("https://api.dropboxapi.com/oauth2/token", tokenRequest);
        if (!tokenRes.IsSuccessStatusCode) return Unauthorized("Error al obtener token de Dropbox.");

        using var tokenDoc = JsonDocument.Parse(await tokenRes.Content.ReadAsStringAsync());
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();

        var userReq = new HttpRequestMessage(HttpMethod.Post, "https://api.dropboxapi.com/2/users/get_current_account");
        userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var userRes = await client.SendAsync(userReq);
        if (!userRes.IsSuccessStatusCode) return Unauthorized("Error al obtener perfil de Dropbox.");

        using var userDoc = JsonDocument.Parse(await userRes.Content.ReadAsStringAsync());

        var id = userDoc.RootElement.GetProperty("account_id").GetString() ?? "";
        var name = userDoc.RootElement.GetProperty("name").GetProperty("display_name").GetString() ?? "UsuarioDropbox";
        var email = userDoc.RootElement.TryGetProperty("email", out var em) ? em.GetString() ?? "" : "";

        await IniciarSesionCookie(id, name, email, "Dropbox");
        return Redirect("/login.html");
    }

    // ================= FIGMA (LISTA MEDIA) =================
    [HttpGet("figma/login")]
    public IActionResult FigmaLogin()
    {
        var clientId = Environment.GetEnvironmentVariable("FIGMA_CLIENT_ID");
        var redirectUri = Uri.EscapeDataString(Environment.GetEnvironmentVariable("FIGMA_REDIRECT_URI") ?? "");
        var url = $"https://www.figma.com/oauth?client_id={clientId}&redirect_uri={redirectUri}&scope=current_user:read&state=redsocial777&response_type=code";
        return Redirect(url);
    }

    [HttpGet("figma/callback")]
    public async Task<IActionResult> FigmaCallback([FromQuery] string? code)
    {
        if (string.IsNullOrEmpty(code)) return BadRequest("No se recibió el código de Figma.");

        var clientId = Environment.GetEnvironmentVariable("FIGMA_CLIENT_ID") ?? "";
        var clientSecret = Environment.GetEnvironmentVariable("FIGMA_CLIENT_SECRET") ?? "";
        var redirectUri = Environment.GetEnvironmentVariable("FIGMA_REDIRECT_URI") ?? "";

        var client = _httpClientFactory.CreateClient();
        var tokenReq = new HttpRequestMessage(HttpMethod.Post, "https://api.figma.com/v1/oauth/token");
        var basicAuth = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        tokenReq.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
        tokenReq.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["redirect_uri"] = redirectUri,
            ["code"] = code,
            ["grant_type"] = "authorization_code"
        });

        var tokenRes = await client.SendAsync(tokenReq);
        if (!tokenRes.IsSuccessStatusCode)
        {
            var err = await tokenRes.Content.ReadAsStringAsync();
            return Unauthorized($"Error al obtener token de Figma: {err}");
        }

        using var tokenDoc = JsonDocument.Parse(await tokenRes.Content.ReadAsStringAsync());
        var accessToken = tokenDoc.RootElement.GetProperty("access_token").GetString();

        var userReq = new HttpRequestMessage(HttpMethod.Get, "https://api.figma.com/v1/me");
        userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var userRes = await client.SendAsync(userReq);
        if (!userRes.IsSuccessStatusCode) return Unauthorized("Error al obtener perfil de Figma.");

        using var userDoc = JsonDocument.Parse(await userRes.Content.ReadAsStringAsync());

        var id = userDoc.RootElement.TryGetProperty("id", out var idProp) ? idProp.ToString() : "";
        var name = userDoc.RootElement.TryGetProperty("handle", out var hProp) && hProp.ValueKind == JsonValueKind.String
            ? hProp.GetString() ?? "UsuarioFigma"
            : "UsuarioFigma";
        var email = userDoc.RootElement.TryGetProperty("email", out var emProp) && emProp.ValueKind == JsonValueKind.String
            ? emProp.GetString() ?? ""
            : "";

        await IniciarSesionCookie(id, name, email, "Figma");
        return Redirect("/login.html");
    }

    // ================= IDENTIDAD Y LOGOUT =================
    [HttpGet("me")]
    public IActionResult Me()
    {
        if (User.Identity?.IsAuthenticated != true)
            return Unauthorized(new { autenticado = false, mensaje = "No hay sesión activa." });

        return Ok(new
        {
            autenticado = true,
            proveedor = User.FindFirst("proveedor")?.Value,
            id = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            nombre = User.Identity.Name,
            email = User.FindFirst(ClaimTypes.Email)?.Value
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { mensaje = "Sesión cerrada correctamente." });
    }

    private async Task IniciarSesionCookie(string id, string nombre, string email, string proveedor)
    {
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim(ClaimTypes.Name, nombre),
            new Claim(ClaimTypes.Email, email),
            new Claim("proveedor", proveedor)
        };
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));}
}
