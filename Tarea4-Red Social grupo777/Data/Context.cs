using Microsoft.EntityFrameworkCore;

public class Context:DbContext
{
    public Context(DbContextOptions<Context> options):base(options){
    }
    public DbSet<Usuario> Usuarios {get;set;}
    public DbSet<Mensaje> Mensajes { get; set; }
    public DbSet<Comentario> Comentarios { get; set; }
    public DbSet<Reaccion> Reacciones { get; set; }
    public DbSet<Hashtag> Hashtags { get; set; }
}