public class EtiquetaPublicacion
{
    public int Id { get; set; }

    public int PublicacionId { get; set; }

    public string Nombre { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; } = DateTime.UtcNow;

    public bool Activo { get; set; } = true;
}