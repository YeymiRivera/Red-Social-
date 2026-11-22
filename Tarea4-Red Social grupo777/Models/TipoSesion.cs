using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public class CuentaExterna
{
    [Key]
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string ProveedorId { get; set; }

    public string Proveedor { get; set; }

    [ForeignKey("UsuarioId")]
    public Usuario Usuario { get; set; }
}