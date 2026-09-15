using Microsoft.EntityFrameworkCore;
using Mindzyxs_API.Repositories.Models;

namespace Mindzyxs_API.Repositories.Models;

public class MindzyxsDbContext(DbContextOptions<MindzyxsDbContext> options) : DbContext(options)
{
    public DbSet<RolRow> Roles => Set<RolRow>();
    public DbSet<GeneroRow> Generos => Set<GeneroRow>();
    public DbSet<EstadoRow> Estados => Set<EstadoRow>();
    public DbSet<UsuarioRow> Usuarios => Set<UsuarioRow>();
    public DbSet<CodigoRow> Codigos => Set<CodigoRow>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<RolRow>(e =>
        {
            e.ToTable("roles", "dbo").HasKey(x => x.IdRol);
            e.Property(x => x.IdRol).HasColumnName("id_rol");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(50).IsUnicode(false);
        });
        b.Entity<GeneroRow>(e =>
        {
            e.ToTable("generos", "dbo").HasKey(x => x.IdGenero);
            e.Property(x => x.IdGenero).HasColumnName("id_genero");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(50).IsUnicode(false);
        });
        b.Entity<EstadoRow>(e =>
        {
            e.ToTable("estados", "dbo").HasKey(x => x.IdEstado);
            e.Property(x => x.IdEstado).HasColumnName("id_estado");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(50).IsUnicode(false);
        });
        b.Entity<UsuarioRow>(e =>
        {
            e.ToTable("usuarios", "dbo").HasKey(x => x.IdUsuario);
            e.Property(x => x.IdUsuario).HasColumnName("id_usuario");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.Apellido).HasColumnName("apellido").HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.Correo).HasColumnName("correo").HasMaxLength(150).IsUnicode(false);
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255).IsUnicode(false);
            e.Property(x => x.FechaNacimiento).HasColumnName("fecha_nacimiento");
            e.Property(x => x.IdRol).HasColumnName("id_rol");
            e.Property(x => x.IdEstadoCuenta).HasColumnName("id_estado_cuenta");
            e.Property(x => x.IdEstadoAprobacion).HasColumnName("id_estado_aprobacion");
            e.Property(x => x.IdGenero).HasColumnName("id_genero");
            e.Property(x => x.FilePath).HasColumnName("file_path").HasMaxLength(500).IsUnicode(false);
            e.Property(x => x.FechaRegistro).HasColumnName("fecha_registro");
            e.HasIndex(x => x.Correo).IsUnique();
        });
        b.Entity<CodigoRow>(e =>
        {
            e.ToTable("codigos_verificacion", "dbo").HasKey(x => x.IdCodigo);
            e.Property(x => x.IdCodigo).HasColumnName("id_codigo");
            e.Property(x => x.IdUsuario).HasColumnName("id_usuario");
            e.Property(x => x.Codigo).HasColumnName("codigo").HasMaxLength(6).IsUnicode(false);
            e.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.ExpiraEn).HasColumnName("expira_en");
            e.Property(x => x.CreadoEn).HasColumnName("creado_en");
        });
    }
}
