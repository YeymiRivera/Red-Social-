using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<Context>(options => options.UseNpgsql(
    builder.Configuration.GetConnectionString("Context")));
builder.Services.AddHttpClient();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "RedSocialAuth";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return Task.CompletedTask;
        };
    });
builder.Services.AddAuthorization();
builder.Services.AddOpenApi();
builder.Services.AddControllers();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<Context>();
    db.Database.Migrate();
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();

app.MapOpenApi();
app.MapScalarApiReference();
app.MapControllers();
app.Run();
