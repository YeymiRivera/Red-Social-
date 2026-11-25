using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly Context _context;
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IDataProtector _estadoProtector;
    private readonly IDataProtector _ticketProtector;
    private readonly IDistributedCache _cache;

    public AuthController(
        Context context,
        IConfiguration configuration,
        IHttpClientFactory httpClientFactory,
        IDataProtectionProvider dataProtection,
        IDistributedCache cache)
    {
        _context = context;
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
        _estadoProtector = dataProtection.CreateProtector("RedSocial.OAuthState.v1");
        _ticketProtector = dataProtection.CreateProtector("RedSocial.LoginTicket.v1");
        _cache = cache;
    }

    [HttpGet("GoogleLogin")]
    public IActionResult GoogleLogin([FromQuery] string? returnUrl)
    {
        if (!GoogleConfigurado())
        {
            return VolverAlLogin(
                "Google todavía no está configurado. Agrega Google:ClientId y Google:ClientSecret.");
        }

        var propiedades = new AuthenticationProperties
        {
            RedirectUri = Url.Action(nameof(GoogleResponse), "Auth")
        };

        var destino = NormalizarReturnUrl(returnUrl);

        if (destino != null)
        {
            propiedades.Items["returnUrl"] = destino;
        }

        return Challenge(propiedades, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("GoogleResponse")]
    public async Task<IActionResult> GoogleResponse()
    {
        var resultado = await HttpContext.AuthenticateAsync("External");

        if (!resultado.Succeeded || resultado.Principal == null)
        {
            return VolverAlLogin("No se pudo completar el inicio de sesión con Google.");
        }

        var identidad = resultado.Principal;

        var googleId = identidad.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var email = identidad.FindFirst(ClaimTypes.Email)?.Value;
        var nombre = identidad.FindFirst(ClaimTypes.Name)?.Value;

        if (string.IsNullOrWhiteSpace(googleId) ||
            string.IsNullOrWhiteSpace(email))
        {
            return VolverAlLogin("Google no proporcionó los datos necesarios.");
        }

        var usuario = await ObtenerOCrearUsuario(
            "Google", googleId, nombre, email);

        var returnUrl =
            resultado.Properties?.Items.TryGetValue("returnUrl", out var destino) == true
                ? NormalizarReturnUrl(destino)
                : null;

        await IniciarSesionCookie(usuario, "Google");
        await HttpContext.SignOutAsync("External");

        return RedirigirDespuesLogin(returnUrl);
    }
    // ================= GITHUB =================
    [HttpGet("GithubLogin")]
    public IActionResult GithubLogin([FromQuery] string? returnUrl)
    {
        var clientId = Setting("GitHub:ClientId", "GITHUB_CLIENT_ID");
        var clientSecret = Setting("GitHub:ClientSecret", "GITHUB_CLIENT_SECRET");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return VolverAlLogin(
                "GitHub todavía no está configurado. Agrega GitHub:ClientId y GitHub:ClientSecret.");
        }

        var redirectUri = ProviderRedirectUri("GitHub", "/api/auth/callback");
        var state = CrearState("github", returnUrl);

        var url = "https://github.com/login/oauth/authorize" +
                  $"?client_id={Uri.EscapeDataString(clientId)}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  "&scope=read:user%20user:email" +
                  $"&state={Uri.EscapeDataString(state)}";

        return Redirect(url);
    }

    [HttpGet("callback")]
    public async Task<IActionResult> GithubCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return VolverAlLogin(
                $"GitHub rechazó el inicio de sesión: {errorDescription ?? error}");
        }

        if (!ValidarState("github", state))
        {
            return VolverAlLogin(
                "La sesión de autorización de GitHub expiró. Intenta de nuevo.");
        }
        var destino = TomarReturnUrl("github");
        if (string.IsNullOrWhiteSpace(code))
        {
            return VolverAlLogin("GitHub no envió el código de autorización.");
        }

        var clientId = Setting("GitHub:ClientId", "GITHUB_CLIENT_ID");
        var clientSecret = Setting("GitHub:ClientSecret", "GITHUB_CLIENT_SECRET");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return VolverAlLogin("GitHub todavía no está configurado.");
        }

        var redirectUri = ProviderRedirectUri("GitHub", "/api/auth/callback");
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));

        var respuestaToken = await client.PostAsync(
            "https://github.com/login/oauth/access_token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            }));

        if (!respuestaToken.IsSuccessStatusCode)
        {
            return VolverAlLogin("No se pudo obtener el token de GitHub.");
        }

        using var documentoToken = JsonDocument.Parse(
            await respuestaToken.Content.ReadAsStringAsync());

        if (!documentoToken.RootElement.TryGetProperty("access_token", out var tokenProp))
        {
            var detalle = documentoToken.RootElement.TryGetProperty("error_description", out var d)
                ? d.GetString()
                : "GitHub no proporcionó un token.";

            return VolverAlLogin(detalle ?? "GitHub no proporcionó un token.");
        }

        var token = tokenProp.GetString();

        if (string.IsNullOrWhiteSpace(token))
        {
            return VolverAlLogin("GitHub no proporcionó un token.");
        }

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("RedSocialApp");
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var respuestaUsuario = await client.GetAsync("https://api.github.com/user");

        if (!respuestaUsuario.IsSuccessStatusCode)
        {
            return VolverAlLogin("No se pudo obtener el perfil de GitHub.");
        }

        using var documento = JsonDocument.Parse(
            await respuestaUsuario.Content.ReadAsStringAsync());

        var root = documento.RootElement;

        if (!root.TryGetProperty("id", out var idProp))
        {
            return VolverAlLogin("GitHub no envió el identificador del usuario.");
        }

        var githubId = idProp.GetInt64().ToString();
        var username = root.TryGetProperty("login", out var loginProp)
            ? loginProp.GetString()
            : null;
        var nombre = root.TryGetProperty("name", out var nombreProp)
            ? nombreProp.GetString()
            : null;

        var email = root.TryGetProperty("email", out var emailProp)
            ? emailProp.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(email))
        {
            email = await ObtenerEmailGitHub(client);
        }

        var usuario = await ObtenerOCrearUsuario(
            "GitHub",
            githubId,
            nombre ?? username,
            email);

        return await FinalizarLogin(usuario, "GitHub", destino);
    }

    // ================= DROPBOX =================
    [HttpGet("dropbox/login")]
    public IActionResult DropboxLogin([FromQuery] string? returnUrl)
    {
        var clientId = Setting("Dropbox:ClientId", "DROPBOX_CLIENT_ID");
        var clientSecret = Setting("Dropbox:ClientSecret", "DROPBOX_CLIENT_SECRET");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return VolverAlLogin(
                "Dropbox todavía no está configurado. Agrega Dropbox:ClientId y Dropbox:ClientSecret.");
        }

        var redirectUri = ProviderRedirectUri("Dropbox", "/auth/dropbox/callback");
        var state = CrearState("dropbox", returnUrl);

        var url = "https://www.dropbox.com/oauth2/authorize" +
                  $"?client_id={Uri.EscapeDataString(clientId)}" +
                  "&response_type=code" +
                  "&token_access_type=online" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&state={Uri.EscapeDataString(state)}";

        return Redirect(url);
    }

    [HttpGet("dropbox/callback")]
    public Task<IActionResult> DropboxCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        return ProcesarDropbox(code, state, error, errorDescription);
    }

    // Ruta registrada en la app de Dropbox: /auth/dropbox/callback
    [HttpGet("/auth/dropbox/callback")]
    public Task<IActionResult> DropboxCallbackLegacy(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        return ProcesarDropbox(code, state, error, errorDescription);
    }

    private async Task<IActionResult> ProcesarDropbox(
        string? code,
        string? state,
        string? error,
        string? errorDescription)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return VolverAlLogin(
                $"Dropbox rechazó el inicio de sesión: {errorDescription ?? error}");
        }

        if (!ValidarState("dropbox", state))
        {
            return VolverAlLogin(
                "La sesión de autorización de Dropbox expiró. Intenta de nuevo.");
        }
        var destino = TomarReturnUrl("dropbox");
        if (string.IsNullOrWhiteSpace(code))
        {
            return VolverAlLogin("Dropbox no envió el código de autorización.");
        }

        var clientId = Setting("Dropbox:ClientId", "DROPBOX_CLIENT_ID");
        var clientSecret = Setting("Dropbox:ClientSecret", "DROPBOX_CLIENT_SECRET");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return VolverAlLogin("Dropbox todavía no está configurado.");
        }

        var redirectUri = ProviderRedirectUri("Dropbox", "/auth/dropbox/callback");
        var client = _httpClientFactory.CreateClient();

        var tokenRes = await client.PostAsync(
            "https://api.dropboxapi.com/oauth2/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = redirectUri
            }));

        if (!tokenRes.IsSuccessStatusCode)
        {
            return VolverAlLogin("No se pudo obtener el token de Dropbox.");
        }

        using var tokenDoc = JsonDocument.Parse(
            await tokenRes.Content.ReadAsStringAsync());

        var accessToken = tokenDoc.RootElement
            .TryGetProperty("access_token", out var tokenProp)
            ? tokenProp.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return VolverAlLogin("Dropbox no proporcionó un token.");
        }

        var userReq = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.dropboxapi.com/2/users/get_current_account");
        userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var userRes = await client.SendAsync(userReq);

        if (!userRes.IsSuccessStatusCode)
        {
            return VolverAlLogin("No se pudo obtener el perfil de Dropbox.");
        }

        using var userDoc = JsonDocument.Parse(
            await userRes.Content.ReadAsStringAsync());

        var id = userDoc.RootElement
            .TryGetProperty("account_id", out var idProp)
            ? idProp.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(id))
        {
            return VolverAlLogin("Dropbox no envió el identificador del usuario.");
        }

        var name = userDoc.RootElement.TryGetProperty("name", out var nameProp) &&
                   nameProp.TryGetProperty("display_name", out var displayProp)
            ? displayProp.GetString()
            : null;

        var email = userDoc.RootElement.TryGetProperty("email", out var emailProp)
            ? emailProp.GetString()
            : null;

        var usuario = await ObtenerOCrearUsuario("Dropbox", id, name, email);
        return await FinalizarLogin(usuario, "Dropbox", destino);
    }

    // ================= FIGMA =================
    [HttpGet("figma/login")]
    public IActionResult FigmaLogin([FromQuery] string? returnUrl)
    {
        var clientId = Setting("Figma:ClientId", "FIGMA_CLIENT_ID");
        var clientSecret = Setting("Figma:ClientSecret", "FIGMA_CLIENT_SECRET");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return VolverAlLogin(
                "Figma todavía no está configurado. Agrega Figma:ClientId y Figma:ClientSecret.");
        }

        var redirectUri = ProviderRedirectUri("Figma", "/auth/figma/callback");
        var state = CrearState("figma", returnUrl);

        var url = "https://www.figma.com/oauth" +
                  $"?client_id={Uri.EscapeDataString(clientId)}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  "&scope=current_user:read" +
                  $"&state={Uri.EscapeDataString(state)}" +
                  "&response_type=code";

        return Redirect(url);
    }

    [HttpGet("figma/callback")]
    public Task<IActionResult> FigmaCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        return ProcesarFigma(code, state, error, errorDescription);
    }

    // Ruta registrada en la app de Figma: /auth/figma/callback
    [HttpGet("/auth/figma/callback")]
    public Task<IActionResult> FigmaCallbackLegacy(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        return ProcesarFigma(code, state, error, errorDescription);
    }

    private async Task<IActionResult> ProcesarFigma(
        string? code,
        string? state,
        string? error,
        string? errorDescription)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return VolverAlLogin(
                $"Figma rechazó el inicio de sesión: {errorDescription ?? error}");
        }

        if (!ValidarState("figma", state))
        {
            return VolverAlLogin(
                "La sesión de autorización de Figma expiró. Intenta de nuevo.");
        }
        var destino = TomarReturnUrl("figma");
        if (string.IsNullOrWhiteSpace(code))
        {
            return VolverAlLogin("Figma no envió el código de autorización.");
        }

        var clientId = Setting("Figma:ClientId", "FIGMA_CLIENT_ID");
        var clientSecret = Setting("Figma:ClientSecret", "FIGMA_CLIENT_SECRET");

        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(clientSecret))
        {
            return VolverAlLogin("Figma todavía no está configurado.");
        }

        var redirectUri = ProviderRedirectUri("Figma", "/auth/figma/callback");
        var client = _httpClientFactory.CreateClient();

        var tokenReq = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.figma.com/v1/oauth/token");

        var basicAuth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

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
            return VolverAlLogin("No se pudo obtener el token de Figma.");
        }

        using var tokenDoc = JsonDocument.Parse(
            await tokenRes.Content.ReadAsStringAsync());

        var accessToken = tokenDoc.RootElement
            .TryGetProperty("access_token", out var tokenProp)
            ? tokenProp.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return VolverAlLogin("Figma no proporcionó un token.");
        }

        var userReq = new HttpRequestMessage(
            HttpMethod.Get,
            "https://api.figma.com/v1/me");
        userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var userRes = await client.SendAsync(userReq);

        if (!userRes.IsSuccessStatusCode)
        {
            return VolverAlLogin("No se pudo obtener el perfil de Figma.");
        }

        using var userDoc = JsonDocument.Parse(
            await userRes.Content.ReadAsStringAsync());

        var id = userDoc.RootElement.TryGetProperty("id", out var idProp)
            ? (idProp.ValueKind == JsonValueKind.String ? idProp.GetString() : idProp.ToString())
            : null;

        if (string.IsNullOrWhiteSpace(id))
        {
            return VolverAlLogin("Figma no envió el identificador del usuario.");
        }

        var name = userDoc.RootElement.TryGetProperty("handle", out var handleProp) &&
                   handleProp.ValueKind == JsonValueKind.String
            ? handleProp.GetString()
            : null;

        var email = userDoc.RootElement.TryGetProperty("email", out var emailProp) &&
                    emailProp.ValueKind == JsonValueKind.String
            ? emailProp.GetString()
            : null;

        var usuario = await ObtenerOCrearUsuario("Figma", id, name, email);
        return await FinalizarLogin(usuario, "Figma", destino);
    }

    // ================= X (TWITTER) =================
    [HttpGet("twitter/login")]
    public IActionResult TwitterLogin([FromQuery] string? returnUrl)
    {
        var clientId = Setting("Twitter:ClientId", "TWITTER_CLIENT_ID");
        var clientSecret = Setting("Twitter:ClientSecret", "TWITTER_CLIENT_SECRET");

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret))
        {
            return VolverAlLogin(
                "X (Twitter) todavía no está configurado. Agrega Twitter:ClientId y Twitter:ClientSecret.");
        }

        var redirectUri = ProviderRedirectUri("Twitter", "/auth/twitter/callback");
        var state = CrearState("twitter", returnUrl);

        // X exige PKCE (S256) para OAuth 2.0. El verifier se guarda en sesión y
        // se vuelve a enviar al canjear el código por el token.
        var codeVerifier = CrearCodeVerifier();
        HttpContext.Session.SetString("oauth_pkce_twitter", codeVerifier);
        var codeChallenge = CrearCodeChallenge(codeVerifier);

        var url = "https://twitter.com/i/oauth2/authorize" +
                  "?response_type=code" +
                  $"&client_id={Uri.EscapeDataString(clientId)}" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  "&scope=users.read%20tweet.read" +
                  $"&state={Uri.EscapeDataString(state)}" +
                  $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                  "&code_challenge_method=S256";

        return Redirect(url);
    }

    [HttpGet("twitter/callback")]
    public Task<IActionResult> TwitterCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        return ProcesarTwitter(code, state, error, errorDescription);
    }

    // Ruta que se registra en el portal de X: /auth/twitter/callback
    [HttpGet("/auth/twitter/callback")]
    public Task<IActionResult> TwitterCallbackLegacy(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription)
    {
        return ProcesarTwitter(code, state, error, errorDescription);
    }

    private async Task<IActionResult> ProcesarTwitter(
        string? code,
        string? state,
        string? error,
        string? errorDescription)
    {
        if (!string.IsNullOrWhiteSpace(error))
        {
            return VolverAlLogin(
                $"X (Twitter) rechazó el inicio de sesión: {errorDescription ?? error}");
        }

        if (!ValidarState("twitter", state))
        {
            return VolverAlLogin(
                "La sesión de autorización de X (Twitter) expiró. Intenta de nuevo.");
        }
        var destino = TomarReturnUrl("twitter");
        if (string.IsNullOrWhiteSpace(code))
        {
            return VolverAlLogin("X (Twitter) no envió el código de autorización.");
        }

        var codeVerifier = HttpContext.Session.GetString("oauth_pkce_twitter");
        HttpContext.Session.Remove("oauth_pkce_twitter");

        if (string.IsNullOrWhiteSpace(codeVerifier))
        {
            return VolverAlLogin(
                "La sesión de autorización de X (Twitter) expiró. Intenta de nuevo.");
        }

        var clientId = Setting("Twitter:ClientId", "TWITTER_CLIENT_ID");
        var clientSecret = Setting("Twitter:ClientSecret", "TWITTER_CLIENT_SECRET");

        if (string.IsNullOrWhiteSpace(clientId) ||
            string.IsNullOrWhiteSpace(clientSecret))
        {
            return VolverAlLogin("X (Twitter) todavía no está configurado.");
        }

        var redirectUri = ProviderRedirectUri("Twitter", "/auth/twitter/callback");
        var client = _httpClientFactory.CreateClient();

        var tokenReq = new HttpRequestMessage(
            HttpMethod.Post,
            "https://api.twitter.com/2/oauth2/token");

        var basicAuth = Convert.ToBase64String(
            Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));

        tokenReq.Headers.Authorization = new AuthenticationHeaderValue("Basic", basicAuth);
        tokenReq.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = redirectUri,
            ["code_verifier"] = codeVerifier
        });

        var tokenRes = await client.SendAsync(tokenReq);

        if (!tokenRes.IsSuccessStatusCode)
        {
            return VolverAlLogin(
                "No se pudo obtener el token de X (Twitter). " + await DetalleError(tokenRes));
        }

        using var tokenDoc = JsonDocument.Parse(
            await tokenRes.Content.ReadAsStringAsync());

        var accessToken = tokenDoc.RootElement
            .TryGetProperty("access_token", out var tokenProp)
            ? tokenProp.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return VolverAlLogin("X (Twitter) no proporcionó un token.");
        }

        var userReq = new HttpRequestMessage(
            HttpMethod.Get,
            "https://api.twitter.com/2/users/me");

        userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var userRes = await client.SendAsync(userReq);

        if (!userRes.IsSuccessStatusCode)
        {
            return VolverAlLogin("No se pudo obtener el perfil de X (Twitter).");
        }

        using var userDoc = JsonDocument.Parse(
            await userRes.Content.ReadAsStringAsync());

        if (!userDoc.RootElement.TryGetProperty("data", out var data) ||
            data.ValueKind != JsonValueKind.Object)
        {
            return VolverAlLogin("X (Twitter) no envió los datos del usuario.");
        }

        var id = data.TryGetProperty("id", out var idProp) &&
                 idProp.ValueKind == JsonValueKind.String
            ? idProp.GetString()
            : null;

        if (string.IsNullOrWhiteSpace(id))
        {
            return VolverAlLogin("X (Twitter) no envió el identificador del usuario.");
        }

        var name = data.TryGetProperty("name", out var nameProp) &&
                   nameProp.ValueKind == JsonValueKind.String
            ? nameProp.GetString()
            : null;

        var username = data.TryGetProperty("username", out var usernameProp) &&
                       usernameProp.ValueKind == JsonValueKind.String
            ? usernameProp.GetString()
            : null;

        var usuario = await ObtenerOCrearUsuario(
            "X",
            id,
            name ?? username,
            null);

        return await FinalizarLogin(usuario, "X", destino);
    }

    // ================= TIKTOK =================
    [HttpGet("tiktok/login")]
    public IActionResult TikTokLogin([FromQuery] string? returnUrl)
    {
        var clientKey = Setting("TikTok:ClientKey", "TIKTOK_CLIENT_KEY");
        var clientSecret = Setting("TikTok:ClientSecret", "TIKTOK_CLIENT_SECRET");

        if (string.IsNullOrWhiteSpace(clientKey) ||
            string.IsNullOrWhiteSpace(clientSecret))
        {
            return VolverAlLogin(
                "TikTok todavía no está configurado. Solicita las credenciales del Sandbox a Gerson.");
        }

        var redirectUri = NormalizarCallbackTikTok(
            Setting("TikTok:RedirectUri", "TIKTOK_REDIRECT_URI"));

        if (string.IsNullOrWhiteSpace(redirectUri) ||
            !redirectUri.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return VolverAlLogin(
                "TikTok requiere un callback HTTPS público. Configura TIKTOK_REDIRECT_URI.");
        }

        var state = CrearStateFirmado(returnUrl);

        var url = "https://www.tiktok.com/v2/auth/authorize/" +
                  $"?client_key={Uri.EscapeDataString(clientKey)}" +
                  "&response_type=code" +
                  "&scope=user.info.basic" +
                  $"&redirect_uri={Uri.EscapeDataString(redirectUri)}" +
                  $"&state={Uri.EscapeDataString(state)}";

        return Redirect(url);
    }

    [HttpGet("tiktok/callback")]
    public Task<IActionResult> TikTokCallback(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? scopes,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        [FromQuery] string? ticket)
    {
        return ProcesarTikTok(code, state, error, errorDescription, ticket);
    }

    // Ruta pública que se registra en TikTok Developers (Login Kit, Web).
    [HttpGet("/auth/tiktok/callback")]
    public Task<IActionResult> TikTokCallbackPublic(
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? scopes,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        [FromQuery] string? ticket)
    {
        return ProcesarTikTok(code, state, error, errorDescription, ticket);
    }

    private async Task<IActionResult> ProcesarTikTok(
        string? code,
        string? state,
        string? error,
        string? errorDescription,
        string? ticket)
    {
        // Segunda vuelta: el callback llegó por el túnel HTTPS (ngrok) y nos reenvió
        // aquí con un ticket firmado para crear la cookie en el host del backend.
        if (!string.IsNullOrWhiteSpace(ticket))
        {
            return await CanjearTicketTikTok(ticket);
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            return VolverAlLogin(
                $"TikTok rechazó el inicio de sesión: {errorDescription ?? error}");
        }

        // El state de TikTok va firmado: la cookie de sesión vive en localhost y el
        // callback llega por el dominio HTTPS público, donde esa cookie no existe.
        if (!LeerStateFirmado(state, out var destino))
        {
            return VolverAlLogin(
                "La sesión de autorización de TikTok expiró. Intenta de nuevo.");
        }
        if (string.IsNullOrWhiteSpace(code))
        {
            return VolverAlLogin("TikTok no envió el código de autorización.");
        }

        var clientKey = Setting("TikTok:ClientKey", "TIKTOK_CLIENT_KEY");
        var clientSecret = Setting("TikTok:ClientSecret", "TIKTOK_CLIENT_SECRET");
        var redirectUri = NormalizarCallbackTikTok(
            Setting("TikTok:RedirectUri", "TIKTOK_REDIRECT_URI"));

        if (string.IsNullOrWhiteSpace(clientKey) ||
            string.IsNullOrWhiteSpace(clientSecret) ||
            string.IsNullOrWhiteSpace(redirectUri))
        {
            return VolverAlLogin("TikTok todavía no está configurado.");
        }

        var client = _httpClientFactory.CreateClient();

        var tokenRes = await client.PostAsync(
            "https://open.tiktokapis.com/v2/oauth/token/",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["client_key"] = clientKey,
                ["client_secret"] = clientSecret,
                ["code"] = code,
                ["grant_type"] = "authorization_code",
                ["redirect_uri"] = redirectUri
            }));

        if (!tokenRes.IsSuccessStatusCode)
        {
            return VolverAlLogin(
                "No se pudo obtener el token de TikTok. " + await DetalleError(tokenRes));
        }

        using var tokenDoc = JsonDocument.Parse(
            await tokenRes.Content.ReadAsStringAsync());

        var accessToken = tokenDoc.RootElement
            .TryGetProperty("access_token", out var tokenProp) &&
            tokenProp.ValueKind == JsonValueKind.String
                ? tokenProp.GetString()
                : null;

        var openId = tokenDoc.RootElement
            .TryGetProperty("open_id", out var openIdProp) &&
            openIdProp.ValueKind == JsonValueKind.String
                ? openIdProp.GetString()
                : null;

        if (string.IsNullOrWhiteSpace(accessToken))
        {
            // TikTok responde 200 con {"error":...} cuando el código o el redirect_uri no valen.
            var detalleTikTok = tokenDoc.RootElement.TryGetProperty("error_description", out var ed) &&
                                ed.ValueKind == JsonValueKind.String
                ? ed.GetString()
                : null;

            return VolverAlLogin(
                "TikTok no proporcionó un token de acceso. " + detalleTikTok);
        }

        if (string.IsNullOrWhiteSpace(openId))
        {
            return VolverAlLogin("TikTok no proporcionó el identificador del usuario.");
        }

        var userReq = new HttpRequestMessage(
            HttpMethod.Get,
            "https://open.tiktokapis.com/v2/user/info/?fields=open_id,display_name");

        userReq.Headers.Authorization =
            new AuthenticationHeaderValue("Bearer", accessToken);

        var userRes = await client.SendAsync(userReq);

        if (!userRes.IsSuccessStatusCode)
        {
            return VolverAlLogin("No se pudo obtener el perfil de TikTok.");
        }

        using var userDoc = JsonDocument.Parse(
            await userRes.Content.ReadAsStringAsync());

        string? displayName = null;

        if (userDoc.RootElement.TryGetProperty("data", out var data) &&
            data.ValueKind == JsonValueKind.Object &&
            data.TryGetProperty("user", out var user) &&
            user.ValueKind == JsonValueKind.Object &&
            user.TryGetProperty("display_name", out var nombreProp) &&
            nombreProp.ValueKind == JsonValueKind.String)
        {
            displayName = nombreProp.GetString();
        }

        // TikTok con user.info.basic no entrega correo: se pasa null a propósito.
        var usuario = await ObtenerOCrearUsuario(
            "TikTok",
            openId,
            displayName ?? openId,
            null);

        // Si el callback llegó por el dominio del túnel, la cookie quedaría en ese dominio
        // y /connect/authorize (en BackendUrl) no la vería. Se traspasa con un ticket.
        var backendUrl = _configuration["BackendUrl"];

        if (Uri.TryCreate(backendUrl, UriKind.Absolute, out var backendUri) &&
            !string.Equals(backendUri.Host, Request.Host.Host, StringComparison.OrdinalIgnoreCase))
        {
            var ticketNuevo = CrearTicketTikTok(usuario.Id, destino);

            return Redirect(
                $"{backendUri.GetLeftPart(UriPartial.Authority)}/api/Auth/tiktok/callback" +
                $"?ticket={Uri.EscapeDataString(ticketNuevo)}");
        }

        return await FinalizarLogin(usuario, "TikTok", destino);
    }

    // ================= IDENTIDAD Y LOGOUT =================

    [HttpGet("me")]
    [TokenAuthorize]
    public IActionResult Me()
    {
        if (User.Identity?.IsAuthenticated != true)
        {
            return Unauthorized(new
            {
                autenticado = false,
                mensaje = "Token no válido."
            });
        }

        return Ok(new
        {
            autenticado = true,

            id = User.FindFirst("sub")?.Value,

            nombre = User.FindFirst("name")?.Value,

            email = User.FindFirst("email")?.Value,

            proveedor = User.FindFirst("provider")?.Value ?? "OpenIddict"
        });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { mensaje = "Sesión cerrada correctamente." });
    }

    // ================= HELPERS =================
    private bool GoogleConfigurado()
    {
        var clientId = Setting("Google:ClientId", "GOOGLE_CLIENT_ID");
        var clientSecret = Setting("Google:ClientSecret", "GOOGLE_CLIENT_SECRET");
        return !string.IsNullOrWhiteSpace(clientId) &&
               !string.IsNullOrWhiteSpace(clientSecret);
    }

    private string? Setting(string key, string environmentVariable)
    {
        var value = _configuration[key];

        return !string.IsNullOrWhiteSpace(value)
            ? value
            : Environment.GetEnvironmentVariable(environmentVariable);
    }

    // TikTok valida la redirect_uri registrada con barra final. La normalizamos
    // aquí para que el intercambio de código use exactamente el mismo valor.
    private static string? NormalizarCallbackTikTok(string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
            return null;

        return redirectUri.EndsWith("/", StringComparison.Ordinal)
            ? redirectUri
            : redirectUri + "/";
    }

    private string ProviderRedirectUri(string provider, string defaultPath)
    {
        var configured = _configuration[$"{provider}:RedirectUri"];

        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured;
        }

        var backendUrl = _configuration["BackendUrl"];

        if (string.IsNullOrWhiteSpace(backendUrl))
        {
            throw new InvalidOperationException(
                "BackendUrl no está configurado.");
        }

        return $"{backendUrl.TrimEnd('/')}{defaultPath}";
    }

    private string CrearState(string provider, string? returnUrl)
    {
        var state = Guid.NewGuid().ToString("N");

        HttpContext.Session.SetString(
            $"oauth_state_{provider}",
            state);

        var destino = NormalizarReturnUrl(returnUrl);

        if (destino != null)
        {
            HttpContext.Session.SetString(
                $"oauth_return_{provider}",
                destino);
        }
        else
        {
            HttpContext.Session.Remove($"oauth_return_{provider}");
        }

        return state;
    }

    private bool ValidarState(string provider, string? state)
    {
        var expected = HttpContext.Session.GetString(
            $"oauth_state_{provider}");

        HttpContext.Session.Remove($"oauth_state_{provider}");

        return !string.IsNullOrWhiteSpace(expected) &&
               string.Equals(expected, state, StringComparison.Ordinal);
    }

    private string? TomarReturnUrl(string provider)
    {
        var destino = HttpContext.Session.GetString(
            $"oauth_return_{provider}");

        HttpContext.Session.Remove($"oauth_return_{provider}");

        return NormalizarReturnUrl(destino);
    }

    // ---- state firmado (sin sesión) y tickets de traspaso, usados por TikTok ----
    private string CrearStateFirmado(string? returnUrl)
    {
        var carga = JsonSerializer.Serialize(new EstadoOAuth(
            Guid.NewGuid().ToString("N"),
            NormalizarReturnUrl(returnUrl),
            DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()));

        return _estadoProtector.Protect(carga);
    }

    private bool LeerStateFirmado(string? state, out string? returnUrl)
    {
        returnUrl = null;

        if (string.IsNullOrWhiteSpace(state))
            return false;

        try
        {
            var carga = JsonSerializer.Deserialize<EstadoOAuth>(_estadoProtector.Unprotect(state));

            if (carga == null ||
                DateTimeOffset.UtcNow.ToUnixTimeSeconds() > carga.Exp)
            {
                return false;
            }

            returnUrl = NormalizarReturnUrl(carga.Ret);
            return true;
        }
        catch (System.Security.Cryptography.CryptographicException)
        {
            return false;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private string CrearTicketTikTok(int usuarioId, string? returnUrl)
    {
        var carga = JsonSerializer.Serialize(new TicketLogin(
            Guid.NewGuid().ToString("N"),
            usuarioId,
            NormalizarReturnUrl(returnUrl),
            DateTimeOffset.UtcNow.AddMinutes(2).ToUnixTimeSeconds()));

        return _ticketProtector.Protect(carga);
    }

    private async Task<IActionResult> CanjearTicketTikTok(string ticket)
    {
        TicketLogin? carga;

        try
        {
            carga = JsonSerializer.Deserialize<TicketLogin>(_ticketProtector.Unprotect(ticket));
        }
        catch (Exception ex) when (ex is System.Security.Cryptography.CryptographicException or JsonException)
        {
            return VolverAlLogin("El ticket de inicio de sesión de TikTok no es válido.");
        }

        if (carga == null || DateTimeOffset.UtcNow.ToUnixTimeSeconds() > carga.Exp)
        {
            return VolverAlLogin("El ticket de inicio de sesión de TikTok expiró. Intenta de nuevo.");
        }

        // Un solo uso.
        var clave = $"ticket:{carga.Nonce}";

        if (await _cache.GetStringAsync(clave) != null)
        {
            return VolverAlLogin("El ticket de inicio de sesión de TikTok ya fue usado.");
        }

        await _cache.SetStringAsync(clave, "1", new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
        });

        var usuario = await _context.Usuarios.FindAsync(carga.UsuarioId);

        if (usuario == null)
        {
            return VolverAlLogin("El usuario del ticket no existe.");
        }

        return await FinalizarLogin(usuario, "TikTok", NormalizarReturnUrl(carga.Ret));
    }

    private static async Task<string> DetalleError(HttpResponseMessage respuesta)
    {
        var cuerpo = await respuesta.Content.ReadAsStringAsync();

        return cuerpo.Length > 200 ? cuerpo[..200] : cuerpo;
    }

    private sealed record EstadoOAuth(string Nonce, string? Ret, long Exp);

    private sealed record TicketLogin(string Nonce, int UsuarioId, string? Ret, long Exp);

    private string? NormalizarReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return null;

        return Url.IsLocalUrl(returnUrl)
            ? returnUrl
            : null;
    }

    private static string CrearCodeVerifier()
    {
        Span<byte> bytes = stackalloc byte[32];
        RandomNumberGenerator.Fill(bytes);
        return Base64UrlEncode(bytes);
    }

    private static string CrearCodeChallenge(string codeVerifier)
    {
        var hash = SHA256.HashData(Encoding.ASCII.GetBytes(codeVerifier));
        return Base64UrlEncode(hash);
    }

    private static string Base64UrlEncode(ReadOnlySpan<byte> bytes)
    {
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    private IActionResult VolverAlLogin(string mensaje)
    {
        return Redirect($"/login.html?error={Uri.EscapeDataString(mensaje)}");
    }
    private async Task<IActionResult> FinalizarLogin(
    Usuario usuario,
    string proveedor,
    string? returnUrl)
    {
        await IniciarSesionCookie(usuario, proveedor);

        return RedirigirDespuesLogin(returnUrl);
    }

    private IActionResult RedirigirDespuesLogin(string? returnUrl)
    {
        var destino = NormalizarReturnUrl(returnUrl);

        if (destino != null)
        {
            return Redirect(destino);
        }

        return IrAlPerfil();
    }
    private IActionResult IrAlPerfil()
    {
        var frontendUrl = _configuration["FrontendUrl"];

        if (!string.IsNullOrWhiteSpace(frontendUrl))
        {
            return Redirect($"{frontendUrl.TrimEnd('/')}/Bienvenida/bienvenida.html");
        }

        return Redirect("/Bienvenida/bienvenida.html");
    }

    private async Task<Usuario> ObtenerOCrearUsuario(
        string proveedor,
        string proveedorId,
        string? nombre,
        string? email)
    {
        nombre = string.IsNullOrWhiteSpace(nombre)
            ? $"Usuario {proveedor}"
            : nombre.Trim();

        email = email?.Trim() ?? "";

        var cuenta = await _context.CuentasExternas
            .FirstOrDefaultAsync(c =>
                c.Proveedor == proveedor &&
                c.ProveedorId == proveedorId);

        Usuario? usuario = null;

        if (cuenta != null)
        {
            usuario = await _context.Usuarios.FindAsync(cuenta.UsuarioId);
        }

        if (usuario == null && !string.IsNullOrWhiteSpace(email))
        {
            usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.email.ToLower() == email.ToLower());
        }

        if (usuario == null)
        {
            usuario = new Usuario
            {
                User = nombre,
                email = email,
                Biografia = $"Autenticado con {proveedor}",
                Fecha = DateTime.UtcNow.ToString("O"),
                Estado = "Activo"
            };

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
        }
        else
        {
            usuario.User = nombre;

            // X (Twitter) no entrega correo con el alcance users.read. No se debe
            // borrar el correo que el usuario ya tenía guardado.
            if (!string.IsNullOrWhiteSpace(email))
            {
                usuario.email = email;
            }

            if (string.IsNullOrWhiteSpace(usuario.Biografia))
            {
                usuario.Biografia = $"Autenticado con {proveedor}";
            }

            await _context.SaveChangesAsync();
        }

        if (cuenta == null)
        {
            _context.CuentasExternas.Add(new CuentaExterna
            {
                ProveedorId = proveedorId,
                Proveedor = proveedor,
                UsuarioId = usuario.Id
            });

            await _context.SaveChangesAsync();
        }
        else if (cuenta.UsuarioId != usuario.Id)
        {
            cuenta.UsuarioId = usuario.Id;
            await _context.SaveChangesAsync();
        }

        return usuario;
    }

    private async Task<string?> ObtenerEmailGitHub(HttpClient client)
    {
        var respuesta = await client.GetAsync("https://api.github.com/user/emails");

        if (!respuesta.IsSuccessStatusCode)
        {
            return null;
        }

        using var documento = JsonDocument.Parse(
            await respuesta.Content.ReadAsStringAsync());

        if (documento.RootElement.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var correo in documento.RootElement.EnumerateArray())
        {
            var primario = correo.TryGetProperty("primary", out var primarioProp) &&
                           primarioProp.ValueKind == JsonValueKind.True;

            var verificado = correo.TryGetProperty("verified", out var verificadoProp) &&
                             verificadoProp.ValueKind == JsonValueKind.True;

            if (primario && verificado &&
                correo.TryGetProperty("email", out var emailProp))
            {
                return emailProp.GetString();
            }
        }

        foreach (var correo in documento.RootElement.EnumerateArray())
        {
            var verificado = correo.TryGetProperty("verified", out var verificadoProp) &&
                             verificadoProp.ValueKind == JsonValueKind.True;

            if (verificado && correo.TryGetProperty("email", out var emailProp))
            {
                return emailProp.GetString();
            }
        }

        return null;
    }

    private async Task IniciarSesionCookie(Usuario usuario, string proveedor)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new(ClaimTypes.Name, usuario.User),
            new(ClaimTypes.Email, usuario.email ?? ""),
            new("proveedor", proveedor)
        };

        var identity = new ClaimsIdentity(
            claims,
            CookieAuthenticationDefaults.AuthenticationScheme);

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity));
    }
}
