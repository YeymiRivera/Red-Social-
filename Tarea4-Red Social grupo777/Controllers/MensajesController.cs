using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MensajesController : ControllerBase
{
    private readonly Context _context;

    public MensajesController(Context context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var lista = await _context.Mensajes
            .Where(m => m.Activo)
            .OrderByDescending(m => m.FechaCreacion)
            .ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var mensaje = await _context.Mensajes.FirstOrDefaultAsync(m => m.Id == id && m.Activo);
        if (mensaje == null) return NotFound(new { mensaje = "Mensaje no encontrado." });
        return Ok(mensaje);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Mensaje nuevo)
    {
        if (string.IsNullOrWhiteSpace(nuevo.Contenido))
            return BadRequest(new { mensaje = "El contenido del mensaje es obligatorio." });

        nuevo.Id = 0;
        nuevo.FechaCreacion = DateTime.UtcNow;
        nuevo.Activo = true;
        if (nuevo.UsuarioId <= 0)
            nuevo.UsuarioId = await ObtenerORegistrarUsuarioActualAsync();

        _context.Mensajes.Add(nuevo);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = nuevo.Id }, nuevo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Mensaje editado)
    {
        var mensaje = await _context.Mensajes.FindAsync(id);
        if (mensaje == null || !mensaje.Activo)
            return NotFound(new { mensaje = "Mensaje no encontrado." });

        if (!string.IsNullOrWhiteSpace(editado.Contenido))
            mensaje.Contenido = editado.Contenido;

        await _context.SaveChangesAsync();
        return Ok(mensaje);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var mensaje = await _context.Mensajes.FindAsync(id);
        if (mensaje == null || !mensaje.Activo)
            return NotFound(new { mensaje = "Mensaje no encontrado." });

        mensaje.Activo = false;
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Mensaje eliminado correctamente.", id });
    }

    private async Task<int> ObtenerORegistrarUsuarioActualAsync()
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? "oauth@redsocial.local";
        var nombre = User.Identity?.Name ?? "UsuarioOAuth";

        var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.email == email);
        if (usuario == null)
        {
            usuario = new Usuario
            {
                User = nombre,
                email = email,
                Biografia = $"Autenticado con {User.FindFirst("proveedor")?.Value ?? "OAuth"}",
                Fecha = DateTime.UtcNow.ToString("yyyy-MM-dd"),
                Estado = "Activo"
            };
            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();
        }
        return usuario.Id;
    }
}
