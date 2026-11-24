
using System.ComponentModel.DataAnnotations.Schema;
public class Seguidores
{
    public int UsuarioSeguidorId { get; set; }

    public int UsuarioSeguidoId { get; set; }
    public string Estado { get; set; } = "Activo";

    [ForeignKey("UsuarioSeguidorId")]
    public Usuario UsuarioSeguidor { get; set; } = null!;

    [ForeignKey("UsuarioSeguidoId")]
    public Usuario UsuarioSeguido { get; set; } = null!;
}