public class Publicacion
{
    public int Id { get; set; }

    public int UsuarioId { get; set; }

    public string Contenido { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public bool Activo { get; set; } = true;

    public int CategoriaPublicacionId { get; set; }
}