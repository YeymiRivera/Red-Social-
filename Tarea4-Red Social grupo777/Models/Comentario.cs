public class Comentario
{
    public int Id { get; set; }
    public string Texto { get; set; } = string.Empty;
    public DateTime FechaComentario { get; set; } = DateTime.UtcNow;
    public int MensajeId { get; set; }
    public int UsuarioId { get; set; }
    public bool Activo { get; set; } = true;
}