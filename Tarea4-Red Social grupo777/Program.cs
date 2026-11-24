using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddDbContext<Context>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("Context")));
builder.Services.AddHttpClient();
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var dataProtectionKeysPath = Path.Combine(
    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
    ".aspnet",
    "DataProtection-Keys"
);
Directory.CreateDirectory(dataProtectionKeysPath);
builder.Services.AddDataProtection()
    .SetApplicationName("RedSocial777")
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath));

var authentication = builder.Services.AddAuthentication(
    CookieAuthenticationDefaults.AuthenticationScheme
)
.AddCookie(options =>
{
    options.Cookie.Name = "RedSocial.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.ExpireTimeSpan = TimeSpan.FromHours(2);
    options.SlidingExpiration = true;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
})
.AddCookie("External");

// Solo se registra Google si hay credenciales. Si faltan, el esquema OAuth
// se inicializa en cada petición y tumba toda la API con un error 500.
var googleClientId = builder.Configuration["Google:ClientId"];
var googleClientSecret = builder.Configuration["Google:ClientSecret"];

if (string.IsNullOrWhiteSpace(googleClientId))
{
    googleClientId = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_ID");
}

if (string.IsNullOrWhiteSpace(googleClientSecret))
{
    googleClientSecret = Environment.GetEnvironmentVariable("GOOGLE_CLIENT_SECRET");
}

if (!string.IsNullOrWhiteSpace(googleClientId) &&
    !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authentication.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.CallbackPath = builder.Configuration["Google:CallbackPath"] ?? "/signin-google";
        options.SignInScheme = "External";

        options.Events.OnRedirectToAuthorizationEndpoint = context =>
        {
            var url = QueryHelpers.AddQueryString(
                context.RedirectUri,
                "prompt",
                "select_account"
            );

            context.Response.Redirect(url);
            return Task.CompletedTask;
        };
    });
}

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(
                  "http://localhost:5500",
                  "http://127.0.0.1:5500")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession();
builder.Services.AddOptions<ScalarOptions>().BindConfiguration("Scalar");

var app = builder.Build();

// Configure the HTTP request pipeline.
// Sirve wwwroot/index.html como página pública en "/".
app.UseDefaultFiles();

if (app.Environment.IsDevelopment() || app.Environment.IsProduction())
{
    app.UseStaticFiles();
    app.MapOpenApi();
    app.MapScalarApiReference();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Context>();
    db.Database.Migrate();
}

app.UseStaticFiles();

// Publica la pantalla de login en la raíz: http://localhost:5290/login.html
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "Frontend", "login")),
    RequestPath = ""
});

// Publica el perfil: http://localhost:5290/Bienvenida/bienvenida.html
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "Frontend", "Bienvenida")),
    RequestPath = "/Bienvenida"
});

app.UseRouting();
app.UseCors("Frontend");
app.UseSession();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();
