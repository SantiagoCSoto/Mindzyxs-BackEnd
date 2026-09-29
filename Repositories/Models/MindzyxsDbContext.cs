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
    public DbSet<AsignacionRow> Asignaciones => Set<AsignacionRow>();
    public DbSet<EspecialidadRow> Especialidades => Set<EspecialidadRow>();
    public DbSet<PsicologoEspecialidadRow> PsicologoEspecialidades => Set<PsicologoEspecialidadRow>();
    public DbSet<PreguntaRow> Preguntas => Set<PreguntaRow>();
    public DbSet<OpcionPreguntaRow> OpcionesPregunta => Set<OpcionPreguntaRow>();
    public DbSet<LineaAyudaRow> LineasAyuda => Set<LineaAyudaRow>();
    public DbSet<RecomendacionRow> Recomendaciones => Set<RecomendacionRow>();
    public DbSet<ConversacionRow> Conversaciones => Set<ConversacionRow>();
    public DbSet<EventoRow> Eventos => Set<EventoRow>();
    public DbSet<RespuestaRow> Respuestas => Set<RespuestaRow>();
    public DbSet<ObservacionRow> Observaciones => Set<ObservacionRow>();
    public DbSet<NivelRiesgoRow> NivelesRiesgo => Set<NivelRiesgoRow>();
    public DbSet<TipoEventoRow> TiposEvento => Set<TipoEventoRow>();
    public DbSet<AutorizacionRow> Autorizaciones => Set<AutorizacionRow>();
    public DbSet<RecordatorioRow> Recordatorios => Set<RecordatorioRow>();
    public DbSet<RecordatorioDiaRow> RecordatorioDias => Set<RecordatorioDiaRow>();
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
        b.Entity<AsignacionRow>(e =>
        {
            e.ToTable("asignaciones_cuestionario", "dbo").HasKey(x => x.IdAsignacion);
            e.Property(x => x.IdAsignacion).HasColumnName("id_asignacion");
            e.Property(x => x.IdCuestionario).HasColumnName("id_cuestionario");
            e.Property(x => x.IdPaciente).HasColumnName("id_paciente");
            e.Property(x => x.IdPsicologo).HasColumnName("id_psicologo");
            e.Property(x => x.FechaAsignacion).HasColumnName("fecha_asignacion");
            e.Property(x => x.FechaLimite).HasColumnName("fecha_limite");
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20).IsUnicode(false);
        });
        b.Entity<EspecialidadRow>(e =>
        {
            e.ToTable("especialidades", "dbo").HasKey(x => x.IdEspecialidad);
            e.Property(x => x.IdEspecialidad).HasColumnName("id_especialidad");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(255).IsUnicode(false);
        });
        b.Entity<PsicologoEspecialidadRow>(e =>
        {
            e.ToTable("psicologo_especialidades", "dbo").HasKey(x => new { x.IdPsicologo, x.IdEspecialidad });
            e.Property(x => x.IdPsicologo).HasColumnName("id_psicologo");
            e.Property(x => x.IdEspecialidad).HasColumnName("id_especialidad");
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
        b.Entity<LineaAyudaRow>(e =>
        {
            e.ToTable("lineas_ayuda", "dbo").HasKey(x => x.IdLinea);
            e.Property(x => x.IdLinea).HasColumnName("id_linea");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.Telefono).HasColumnName("telefono").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.Url).HasColumnName("url").HasMaxLength(255).IsUnicode(false);
            e.Property(x => x.Horario).HasColumnName("horario").HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.Cobertura).HasColumnName("cobertura").HasMaxLength(255).IsUnicode(false);
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.Descripcion).HasColumnName("descripcion").HasMaxLength(500).IsUnicode(false);
            e.Property(x => x.Whatsapp).HasColumnName("whatsapp").HasMaxLength(30).IsUnicode(false);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(150).IsUnicode(false);
            e.Property(x => x.ChatUrl).HasColumnName("chat_url").HasMaxLength(255).IsUnicode(false);
            e.Property(x => x.Videollamada).HasColumnName("videollamada").HasMaxLength(255).IsUnicode(false);
            e.Property(x => x.TipoAtencion).HasColumnName("tipo_atencion").HasMaxLength(40).IsUnicode(false);
            e.Property(x => x.Departamento).HasColumnName("departamento").HasMaxLength(60).IsUnicode(false);
            e.Property(x => x.Municipio).HasColumnName("municipio").HasMaxLength(60).IsUnicode(false);
            e.Property(x => x.Poblacion).HasColumnName("poblacion").HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.Fuente).HasColumnName("fuente").HasMaxLength(255).IsUnicode(false);
            e.Property(x => x.FechaVerificacion).HasColumnName("fecha_verificacion");
        });
        b.Entity<RecomendacionRow>(e =>
        {
            e.ToTable("recomendaciones", "dbo").HasKey(x => x.IdRecomendacion);
            e.Property(x => x.IdRecomendacion).HasColumnName("id_recomendacion");
            e.Property(x => x.Titulo).HasColumnName("titulo").HasMaxLength(200).IsUnicode(false);
            e.Property(x => x.Contenido).HasColumnName("contenido").HasColumnType("text").IsUnicode(false);
            e.Property(x => x.NivelRiesgo).HasColumnName("nivel_riesgo").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.Intro).HasColumnName("intro").HasMaxLength(500).IsUnicode(false);
            e.Property(x => x.Pasos).HasColumnName("pasos").HasColumnType("text").IsUnicode(false);
            e.Property(x => x.Duracion).HasColumnName("duracion").HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.Instrucciones).HasColumnName("instrucciones").HasColumnType("text").IsUnicode(false);
            e.Property(x => x.Icono).HasColumnName("icono").HasMaxLength(20);
        });
        b.Entity<ConversacionRow>(e =>
        {
            e.ToTable("conversaciones", "dbo").HasKey(x => x.IdConversacion);
            e.Property(x => x.IdConversacion).HasColumnName("id_conversacion");
            e.Property(x => x.IdPaciente).HasColumnName("id_paciente");
            e.Property(x => x.IdPsicologo).HasColumnName("id_psicologo");
            e.Property(x => x.Estado).HasColumnName("estado").HasMaxLength(20).IsUnicode(false);
            e.Property(x => x.IdBloqueador).HasColumnName("id_bloqueador");
            e.Property(x => x.FechaCreacion).HasColumnName("fecha_creacion");
            e.Property(x => x.FechaRespuesta).HasColumnName("fecha_respuesta");
        });
        b.Entity<EventoRow>(e =>
        {
            e.ToTable("eventos_bienestar", "dbo").HasKey(x => x.IdEvento);
            e.Property(x => x.IdEvento).HasColumnName("id_evento");
            e.Property(x => x.IdPaciente).HasColumnName("id_paciente");
            e.Property(x => x.IdTipoEvento).HasColumnName("id_tipo_evento");
            e.Property(x => x.IdCuestionario).HasColumnName("id_cuestionario");
            e.Property(x => x.IdNivelRiesgo).HasColumnName("id_nivel_riesgo");
            e.Property(x => x.PuntajeTotal).HasColumnName("puntaje_total");
            e.Property(x => x.FechaEvento).HasColumnName("fecha_evento");
            e.Property(x => x.Nota).HasColumnName("nota").HasColumnType("varchar(max)").IsUnicode(false);
        });
        b.Entity<RespuestaRow>(e =>
        {
            e.ToTable("respuestas", "dbo").HasKey(x => x.IdRespuesta);
            e.Property(x => x.IdRespuesta).HasColumnName("id_respuesta");
            e.Property(x => x.IdEvento).HasColumnName("id_evento");
            e.Property(x => x.IdPregunta).HasColumnName("id_pregunta");
            e.Property(x => x.Valor).HasColumnName("valor");
            e.Property(x => x.Texto).HasColumnName("texto").HasMaxLength(500).IsUnicode(false);
            e.Property(x => x.FechaRespuesta).HasColumnName("fecha_respuesta");
        });
        b.Entity<ObservacionRow>(e =>
        {
            e.ToTable("observaciones", "dbo").HasKey(x => x.IdObservacion);
            e.Property(x => x.IdObservacion).HasColumnName("id_observacion");
            e.Property(x => x.IdPsicologo).HasColumnName("id_psicologo");
            e.Property(x => x.IdPaciente).HasColumnName("id_paciente");
            e.Property(x => x.IdEvento).HasColumnName("id_evento");
            e.Property(x => x.Contenido).HasColumnName("contenido").HasColumnType("text").IsUnicode(false);
            e.Property(x => x.Firma).HasColumnName("firma").HasMaxLength(255).IsUnicode(false);
            e.Property(x => x.FechaObservacion).HasColumnName("fecha_observacion");
        });
        b.Entity<NivelRiesgoRow>(e =>
        {
            e.ToTable("niveles_riesgo", "dbo").HasKey(x => x.IdNivelRiesgo);
            e.Property(x => x.IdNivelRiesgo).HasColumnName("id_nivel_riesgo");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(50).IsUnicode(false);
        });
        b.Entity<TipoEventoRow>(e =>
        {
            e.ToTable("tipos_evento", "dbo").HasKey(x => x.IdTipoEvento);
            e.Property(x => x.IdTipoEvento).HasColumnName("id_tipo_evento");
            e.Property(x => x.Nombre).HasColumnName("nombre").HasMaxLength(50).IsUnicode(false);
        });
        b.Entity<AutorizacionRow>(e =>
        {
            e.ToTable("autorizaciones", "dbo").HasKey(x => x.IdAutorizacion);
            e.Property(x => x.IdAutorizacion).HasColumnName("id_autorizacion");
            e.Property(x => x.IdPaciente).HasColumnName("id_paciente");
            e.Property(x => x.IdPsicologo).HasColumnName("id_psicologo");
            e.Property(x => x.IdEstado).HasColumnName("id_estado");
            e.Property(x => x.FechaSolicitud).HasColumnName("fecha_solicitud");
            e.Property(x => x.FechaRespuesta).HasColumnName("fecha_respuesta");
        });
        b.Entity<RecordatorioRow>(e =>
        {
            e.ToTable("recordatorios", "dbo").HasKey(x => x.IdRecordatorio);
            e.Property(x => x.IdRecordatorio).HasColumnName("id_recordatorio");
            e.Property(x => x.IdPaciente).HasColumnName("id_paciente");
            e.Property(x => x.Titulo).HasColumnName("titulo").HasMaxLength(100).IsUnicode(false);
            e.Property(x => x.Mensaje).HasColumnName("mensaje").HasMaxLength(500).IsUnicode(false);
            e.Property(x => x.Hora).HasColumnName("hora");
            e.Property(x => x.Activo).HasColumnName("activo");
            e.Property(x => x.Frecuencia).HasColumnName("frecuencia").HasMaxLength(20).IsUnicode(false);
        });
        b.Entity<RecordatorioDiaRow>(e =>
        {
            e.ToTable("recordatorio_dias", "dbo").HasKey(x => new { x.IdRecordatorio, x.IdDia });
            e.Property(x => x.IdRecordatorio).HasColumnName("id_recordatorio");
            e.Property(x => x.IdDia).HasColumnName("id_dia");
        });
    }
}
