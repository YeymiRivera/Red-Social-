using System.ComponentModel.DataAnnotations.Schema;
public class Bloqueados
{
    public int UsuarioBloqueadorId { get; set; }

    public int UsuarioBloqueadoId { get; set; }
    public string Estado { get; set; } = "Activo";

    [ForeignKey("UsuarioBloqueadorId")]
    public Usuario UsuarioBloqueador { get; set; }

    [ForeignKey("UsuarioBloqueadoId")]
    public Usuario UsuarioBloqueado { get; set; }
}