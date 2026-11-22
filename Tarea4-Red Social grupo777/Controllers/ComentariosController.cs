using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ComentariosController : ControllerBase
{
    private readonly Context _context;

    public ComentariosController(Context context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? mensajeId)
    {
        var query = _context.Comentarios.Where(c => c.Activo);
        if (mensajeId.HasValue)
            query = query.Where(c => c.MensajeId == mensajeId.Value);

        var lista = await query.OrderByDescending(c => c.FechaComentario).ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var comentario = await _context.Comentarios.FirstOrDefaultAsync(c => c.Id == id && c.Activo);
        if (comentario == null) return NotFound(new { mensaje = "Comentario no encontrado." });
        return Ok(comentario);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Comentario nuevo)
    {
        if (string.IsNullOrWhiteSpace(nuevo.Texto))
            return BadRequest(new { mensaje = "El texto del comentario es obligatorio." });

        var mensajeExiste = await _context.Mensajes.AnyAsync(m => m.Id == nuevo.MensajeId && m.Activo);
        if (!mensajeExiste)
            return BadRequest(new { mensaje = $"El MensajeId {nuevo.MensajeId} no existe o está inactivo." });

        nuevo.Id = 0;
        nuevo.FechaComentario = DateTime.UtcNow;
        nuevo.Activo = true;
        if (nuevo.UsuarioId <= 0)
            nuevo.UsuarioId = await ObtenerORegistrarUsuarioActualAsync();

        _context.Comentarios.Add(nuevo);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = nuevo.Id }, nuevo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Comentario editado)
    {
        var comentario = await _context.Comentarios.FindAsync(id);
        if (comentario == null || !comentario.Activo)
            return NotFound(new { mensaje = "Comentario no encontrado." });

        if (!string.IsNullOrWhiteSpace(editado.Texto))
            comentario.Texto = editado.Texto;

        await _context.SaveChangesAsync();
        return Ok(comentario);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var comentario = await _context.Comentarios.FindAsync(id);
        if (comentario == null || !comentario.Activo)
            return NotFound(new { mensaje = "Comentario no encontrado." });

        comentario.Activo = false;
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Comentario eliminado correctamente.", id });
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
