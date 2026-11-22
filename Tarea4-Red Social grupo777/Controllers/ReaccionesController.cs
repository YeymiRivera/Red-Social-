using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ReaccionesController : ControllerBase
{
    private readonly Context _context;

    public ReaccionesController(Context context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? mensajeId)
    {
        var query = _context.Reacciones.Where(r => r.Activo);
        if (mensajeId.HasValue)
            query = query.Where(r => r.MensajeId == mensajeId.Value);

        var lista = await query.OrderByDescending(r => r.FechaReaccion).ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var reaccion = await _context.Reacciones.FirstOrDefaultAsync(r => r.Id == id && r.Activo);
        if (reaccion == null) return NotFound(new { mensaje = "Reacción no encontrada." });
        return Ok(reaccion);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Reaccion nueva)
    {
        if (string.IsNullOrWhiteSpace(nueva.Tipo))
            return BadRequest(new { mensaje = "El tipo de reacción es obligatorio (ej. Like, Me encanta)." });

        var mensajeExiste = await _context.Mensajes.AnyAsync(m => m.Id == nueva.MensajeId && m.Activo);
        if (!mensajeExiste)
            return BadRequest(new { mensaje = $"El MensajeId {nueva.MensajeId} no existe o está inactivo." });

        nueva.Id = 0;
        nueva.FechaReaccion = DateTime.UtcNow;
        nueva.Activo = true;
        if (nueva.UsuarioId <= 0)
            nueva.UsuarioId = await ObtenerORegistrarUsuarioActualAsync();

        _context.Reacciones.Add(nueva);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = nueva.Id }, nueva);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Reaccion editada)
    {
        var reaccion = await _context.Reacciones.FindAsync(id);
        if (reaccion == null || !reaccion.Activo)
            return NotFound(new { mensaje = "Reacción no encontrada." });

        if (!string.IsNullOrWhiteSpace(editada.Tipo))
            reaccion.Tipo = editada.Tipo;

        await _context.SaveChangesAsync();
        return Ok(reaccion);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var reaccion = await _context.Reacciones.FindAsync(id);
        if (reaccion == null || !reaccion.Activo)
            return NotFound(new { mensaje = "Reacción no encontrada." });

        reaccion.Activo = false;
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Reacción eliminada correctamente.", id });
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
