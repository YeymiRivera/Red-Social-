using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    // ================= DROPBOX (LISTA FÁCIL) =================
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
            new ClaimsPrincipal(identity));
    }
}
