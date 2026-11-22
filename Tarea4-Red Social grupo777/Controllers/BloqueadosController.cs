using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
namespace UsuarioApi.Controller;
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BloqueadosController : ControllerBase
{
    private readonly Context _context;

    public BloqueadosController(Context context)
    {
        _context = context;
    }
    [HttpGet("Listar")]
    public async Task<IActionResult> Listar()
    {
        var bloqueados = await (
            from b in _context.Bloqueados
            select b
        ).ToListAsync();

        return Ok(bloqueados);
    }

    // GET: api/Bloqueados/Buscar/1
    [HttpGet("Buscar/{id}")]
    public async Task<IActionResult> Buscar(int id)
    {
        var bloqueado = await (
            from b in _context.Bloqueados
            where b.UsuarioBloqueadoId == id
            select b
        ).FirstOrDefaultAsync();

        if (bloqueado == null)
            return NotFound("Bloqueado no encontrado.");

        return Ok(bloqueado);
    }

    // POST: api/Usuario/Crear
    [HttpPost("Crear")]
    public async Task<IActionResult> Crear(int usuarioBloqueadorId, int usuarioBloqueadoId)
    {
        bool existe = await (
            from b in _context.Bloqueados
            where b.UsuarioBloqueadorId == usuarioBloqueadorId && b.UsuarioBloqueadoId == usuarioBloqueadoId
            select b
        ).AnyAsync();

        if (existe)
            return Conflict("El nombre de usuario ya existe.");

        Bloqueados bloqueado = new Bloqueados();
        bloqueado.UsuarioBloqueadorId = usuarioBloqueadorId;
        bloqueado.UsuarioBloqueadoId = usuarioBloqueadoId;
        _context.Bloqueados.Add(bloqueado);

        await _context.SaveChangesAsync();

        return Ok(bloqueado);
    }
}