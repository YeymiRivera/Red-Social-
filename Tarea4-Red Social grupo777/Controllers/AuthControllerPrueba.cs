
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

[ApiController]
[Route("api/[controller]")]
public class AuthControllerPrueba : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IHttpClientFactory _httpClientFactory;

    public AuthControllerPrueba(IConfiguration configuration, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _httpClientFactory = httpClientFactory;
    }
    [HttpGet("login")]
public async Task<IActionResult> Login()
{
    var clientId = _configuration["GitHub:ClientId"];
    var redirectUri = "http://localhost:5290/api/auth/callback";

    var url = "https://github.com/login/oauth/authorize" +
    $"?client_id={clientId}" +
    $"&redirect_uri={redirectUri}" +
    $"&scope=user";
    return Ok(Redirect(url));
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

    return Ok("Usuario autenticado correctamente");
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