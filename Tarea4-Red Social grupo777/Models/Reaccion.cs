public class Reaccion
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public DateTime FechaReaccion { get; set; } = DateTime.UtcNow;
    public int MensajeId { get; set; }
    public int UsuarioId { get; set; }
    public bool Activo { get; set; } = true;
}