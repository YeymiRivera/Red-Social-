using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace UsuarioApi.Controller;
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly Context _context;

    public UsuarioController(Context context)
    {
        _context = context;
    }

    // GET: api/Usuario/Listar
    [HttpGet("Listar")]
    public async Task<IActionResult> Listar()
    {
        var usuarios = await (
            from u in _context.Usuarios
            select u
        ).ToListAsync();

        return Ok(usuarios);
    }

    // GET: api/Usuario/Buscar/yeymi
    [HttpGet("Buscar/{user}")]
    public async Task<IActionResult> Buscar(string user)
    {
        var usuario = await (
            from u in _context.Usuarios
            where u.User.ToLower() == user.ToLower()
            select u
        ).FirstOrDefaultAsync();

        if (usuario == null)
            return NotFound("Usuario no encontrado.");

        return Ok(usuario);
    }

    // POST: api/Usuario/Crear
    [HttpPost("Crear")]
    public async Task<IActionResult> Crear(string nombre,string email,string biografia)
    {
        bool existe = await (
            from u in _context.Usuarios
            where u.User.ToLower() == nombre.ToLower()
            select u
        ).AnyAsync();

        if (existe)
            return Conflict("El nombre de usuario ya existe.");

        Usuario usuario=new Usuario();
        usuario.Biografia=biografia;
        usuario.User=nombre;
        usuario.email=email;
        usuario.Fecha=DateOnly.FromDateTime(DateTime.Now).ToString();
        usuario.Estado="Activo";
        _context.Usuarios.Add(usuario);

        await _context.SaveChangesAsync();

        return Ok(usuario);
    }
}