using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
namespace UsuarioApi.Controller;
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class CuentasExternasController : ControllerBase
{
    private readonly Context _context;

    public CuentasExternasController(Context context)
    {
        _context = context;
    }
    [HttpGet("Listar")]
    public async Task<IActionResult> Listar()
    {
        var cuentasExternas = await (
            from c in _context.CuentasExternas
            select c
        ).ToListAsync();

        return Ok(cuentasExternas);
    }

    // GET: api/CuentasExternas/Buscar/1
    [HttpGet("Buscar/{id}")]
    public async Task<IActionResult> Buscar(int id)
    {
        var cuentaExterna = await (
            from c in _context.CuentasExternas
            where c.UsuarioId == id
            select c
        ).FirstOrDefaultAsync();

        if (cuentaExterna == null)
            return NotFound("Cuenta externa no encontrada.");

        return Ok(cuentaExterna);
    }

    // POST: api/Usuario/Crear
    [HttpPost("Crear")]
    public async Task<IActionResult> Crear(int usuarioId, string proveedorId, string proveedor)
    {
        bool existe = await (
            from c in _context.CuentasExternas
            where c.UsuarioId == usuarioId && c.ProveedorId == proveedorId
            select c
        ).AnyAsync();

        if (existe)
            return Conflict("El nombre de usuario ya existe.");

        CuentaExterna cuentaExterna=new CuentaExterna();
        cuentaExterna.UsuarioId=usuarioId;
        cuentaExterna.ProveedorId=proveedorId;
        cuentaExterna.Proveedor=proveedor;
        _context.CuentasExternas.Add(cuentaExterna);

        await _context.SaveChangesAsync();

        return Ok(cuentaExterna);
    }
}