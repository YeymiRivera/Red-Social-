using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HashtagsController : ControllerBase
{
    private readonly Context _context;

    public HashtagsController(Context context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var lista = await _context.Hashtags
            .Where(h => h.Activo)
            .OrderByDescending(h => h.FechaCreacion)
            .ToListAsync();
        return Ok(lista);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var hashtag = await _context.Hashtags.FirstOrDefaultAsync(h => h.Id == id && h.Activo);
        if (hashtag == null) return NotFound(new { mensaje = "Hashtag no encontrado." });
        return Ok(hashtag);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] Hashtag nuevo)
    {
        if (string.IsNullOrWhiteSpace(nuevo.Nombre))
            return BadRequest(new { mensaje = "El nombre del hashtag es obligatorio." });

        var limpio = nuevo.Nombre.Trim();
        nuevo.Nombre = limpio.StartsWith("#") ? limpio : $"#{limpio}";
        nuevo.Id = 0;
        nuevo.FechaCreacion = DateTime.UtcNow;
        nuevo.Activo = true;

        _context.Hashtags.Add(nuevo);
        await _context.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = nuevo.Id }, nuevo);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] Hashtag editado)
    {
        var hashtag = await _context.Hashtags.FindAsync(id);
        if (hashtag == null || !hashtag.Activo)
            return NotFound(new { mensaje = "Hashtag no encontrado." });

        if (!string.IsNullOrWhiteSpace(editado.Nombre))
        {
            var limpio = editado.Nombre.Trim();
            hashtag.Nombre = limpio.StartsWith("#") ? limpio : $"#{limpio}";
        }

        await _context.SaveChangesAsync();
        return Ok(hashtag);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var hashtag = await _context.Hashtags.FindAsync(id);
        if (hashtag == null || !hashtag.Activo)
            return NotFound(new { mensaje = "Hashtag no encontrado." });

        hashtag.Activo = false;
        await _context.SaveChangesAsync();
        return Ok(new { mensaje = "Hashtag eliminado correctamente.", id });
    }
}

// Aporte de prueba con conflicto intencional: mismo método POST y misma ruta base
[HttpPost]
public async Task<IActionResult> CreateDuplicado([FromBody] Hashtag duplicado)
{
    duplicado.Id = 0;
    duplicado.FechaCreacion = DateTime.UtcNow;
    duplicado.Activo = true;
    _context.Hashtags.Add(duplicado);
    await _context.SaveChangesAsync();
    return Ok(duplicado);
}
