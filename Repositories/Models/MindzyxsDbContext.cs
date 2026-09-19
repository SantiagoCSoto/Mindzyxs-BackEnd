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
    public DbSet<SesionRow> Sesiones => Set<SesionRow>();
    public DbSet<AuthRow> AuthLogs => Set<AuthRow>();
    public DbSet<RespaldoRow> Respaldos => Set<RespaldoRow>();
    public DbSet<CuestionarioRow> Cuestionarios => Set<CuestionarioRow>();
    public DbSet<PreguntaRow> Preguntas => Set<PreguntaRow>();
    public DbSet<OpcionPreguntaRow> OpcionesPregunta => Set<OpcionPreguntaRow>();

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
            e.Property(x => x.UltimoAcceso).HasColumnName("ultimo_acceso");
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
        b.Entity<SesionRow>(e =>
        {
            e.ToTable("sesiones", "dbo").HasKey(x => x.IdSesion);
            e.Property(x => x.IdSesion).HasColumnName("id_sesion");
            e.Property(x => x.IdUsuario).HasColumnName("id_usuario");
            e.Property(x => x.TokenHash).HasColumnName("token_hash").HasMaxLength(500).IsUnicode(false);
            e.Property(x => x.ExpiraEn).HasColumnName("expira_en");
            e.Property(x => x.CreadaEn).HasColumnName("creada_en");
        });
        b.Entity<AuthRow>(e =>
        {
            e.ToTable("auth", "dbo").HasKey(x => x.IdAuth);
            e.Property(x => x.IdAuth).HasColumnName("id_auth");
            e.Property(x => x.ActorIdUsuario).HasColumnName("actor_id_usuario");
            e.Property(x => x.TargetIdUsuario).HasColumnName("target_id_usuario");
            e.Property(x => x.Accion).HasColumnName("accion").HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.Detalle).HasColumnName("detalle").IsUnicode(false);
            e.Property(x => x.Fecha).HasColumnName("fecha");
            e.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45).IsUnicode(false);
        });
        b.Entity<RespaldoRow>(e =>
        {
            e.ToTable("respaldos", "dbo").HasKey(x => x.IdRespaldo);
            e.Property(x => x.IdRespaldo).HasColumnName("id_respaldo");
            e.Property(x => x.NombreArchivo).HasColumnName("nombre_archivo").HasMaxLength(255).IsUnicode(false);
            e.Property(x => x.Tamano).HasColumnName("tamano").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.FechaHora).HasColumnName("fecha_hora").HasDefaultValueSql("SYSDATETIME()");
            e.Property(x => x.IdAdmin).HasColumnName("id_admin");
        });
        b.Entity<CuestionarioRow>(e =>
        {
            e.ToTable("cuestionarios", "dbo").HasKey(x => x.IdCuestionario);
            e.Property(x => x.IdCuestionario).HasColumnName("id_cuestionario");
            e.Property(x => x.Titulo).HasColumnName("titulo").HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(500).IsUnicode(false);
            e.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.IdCreador).HasColumnName("id_creador");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
        });
        b.Entity<PreguntaRow>(e =>
        {
            e.ToTable("preguntas", "dbo").HasKey(x => x.IdPregunta);
            e.Property(x => x.IdPregunta).HasColumnName("id_pregunta");
            e.Property(x => x.IdCuestionario).HasColumnName("id_cuestionario");
            e.Property(x => x.Texto).HasColumnName("texto").HasMaxLength(500).IsUnicode(false);
            e.Property(x => x.Orden).HasColumnName("orden");
            e.Property(x => x.Tipo).HasColumnName("tipo").HasMaxLength(50).IsUnicode(false);
            e.Property(x => x.PuntajeMax).HasColumnName("puntaje_max");
        });
        b.Entity<OpcionPreguntaRow>(e =>
        {
            e.ToTable("opciones_pregunta", "dbo").HasKey(x => x.IdOpcion);
            e.Property(x => x.IdOpcion).HasColumnName("id_opcion");
            e.Property(x => x.IdPregunta).HasColumnName("id_pregunta");
            e.Property(x => x.Texto).HasColumnName("texto").HasMaxLength(255).IsUnicode(false);
            e.Property(x => x.Valor).HasColumnName("valor");
            e.Property(x => x.Orden).HasColumnName("orden");
        });
    }
}
