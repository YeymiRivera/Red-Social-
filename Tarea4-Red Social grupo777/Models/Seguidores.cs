
using System.ComponentModel.DataAnnotations.Schema;
public class Seguidores
{
    public int UsuarioSeguidorId { get; set; }

    public int UsuarioSeguidoId { get; set; }

    [ForeignKey("UsuarioSeguidorId")]
    public Usuario UsuarioSeguidor { get; set; }

    [ForeignKey("UsuarioSeguidoId")]
    public Usuario UsuarioSeguido { get; set; }
}