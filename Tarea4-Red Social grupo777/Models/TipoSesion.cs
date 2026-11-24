using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public class CuentaExterna
{
    [Key]
    public int Id { get; set; }

    public int UsuarioId { get; set; }
    public string Estado { get; set; } = "Activo";

    public string ProveedorId { get; set; } = string.Empty;

    public string Proveedor { get; set; } = string.Empty;

    [ForeignKey("UsuarioId")]
    public Usuario Usuario { get; set; } = null!;
}