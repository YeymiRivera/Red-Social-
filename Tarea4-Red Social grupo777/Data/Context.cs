using Microsoft.EntityFrameworkCore;

public class Context : DbContext
{
    public Context(DbContextOptions<Context> options) : base(options)
    {
    }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<CuentaExterna> CuentasExternas { get; set; }
    public DbSet<Seguidores> Seguidores { get; set; }
    public DbSet<Bloqueados> Bloqueados { get; set; }
    public DbSet<Mensaje> Mensajes { get; set; }
    public DbSet<Comentario> Comentarios { get; set; }
    public DbSet<Reaccion> Reacciones { get; set; }
    public DbSet<Hashtag> Hashtags { get; set; }
    public DbSet<Publicacion> Publicaciones { get; set; }
    public DbSet<ImagenPublicacion> ImagenesPublicacion { get; set; }
    public DbSet<CategoriaPublicacion> CategoriasPublicacion { get; set; }
    public DbSet<EtiquetaPublicacion> EtiquetasPublicacion { get; set; }
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Seguidores>()
            .HasKey(s => new
            {
                s.UsuarioSeguidorId,
                s.UsuarioSeguidoId
            });

        modelBuilder.Entity<Seguidores>()
            .HasOne(s => s.UsuarioSeguidor)
            .WithMany()
            .HasForeignKey(s => s.UsuarioSeguidorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Seguidores>()
            .HasOne(s => s.UsuarioSeguido)
            .WithMany()
            .HasForeignKey(s => s.UsuarioSeguidoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Bloqueados>()
            .HasKey(b => new
            {
                b.UsuarioBloqueadorId,
                b.UsuarioBloqueadoId
            });
        modelBuilder.Entity<Bloqueados>()
            .HasOne(b => b.UsuarioBloqueador)
            .WithMany()
            .HasForeignKey(b => b.UsuarioBloqueadorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Bloqueados>()
            .HasOne(b => b.UsuarioBloqueado)
            .WithMany()
            .HasForeignKey(b => b.UsuarioBloqueadoId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Publicacion>()
        .HasOne<CategoriaPublicacion>()
        .WithMany()
        .HasForeignKey(p => p.CategoriaPublicacionId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Publicacion>()
        .HasOne<Usuario>()
        .WithMany()
        .HasForeignKey(p => p.UsuarioId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ImagenPublicacion>()
        .HasOne<Publicacion>()
        .WithMany()
        .HasForeignKey(i => i.PublicacionId)
        .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<EtiquetaPublicacion>()
        .HasOne<Publicacion>()
        .WithMany()
        .HasForeignKey(e => e.PublicacionId)
        .OnDelete(DeleteBehavior.Restrict);


        
    }
}