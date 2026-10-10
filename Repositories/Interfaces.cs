using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories.Models;

namespace Mindzyxs_API.Repositories;

public interface IUserRepository
{
    Task<UsuarioDto?> FindByCorreoAsync(string correo, CancellationToken ct = default);
    Task<UsuarioDto?> FindByIdAsync(int idUsuario, CancellationToken ct = default);
    Task<bool> ExistsByCorreoAsync(string correo, CancellationToken ct = default);
    Task<int> AddAsync(UsuarioDto user, CancellationToken ct = default);
    Task SetFilePathAsync(int idUsuario, string path, CancellationToken ct = default);
    Task SetEstadoCuentaAsync(int idUsuario, int idEstado, CancellationToken ct = default);
    Task SetPasswordAsync(int idUsuario, string passwordHash, CancellationToken ct = default);
    Task UpdateUltimoAccesoAsync(int idUsuario, CancellationToken ct = default);
    // PB-10 HU1-PB10: actualiza datos del perfil y correo (con verificación aparte).
    Task ActualizarPerfilAsync(int idUsuario, string nombres, string apellidos, DateTime? fechaNacimiento, int? idGenero, CancellationToken ct = default);
    Task ActualizarCorreoAsync(int idUsuario, string nuevoCorreo, CancellationToken ct = default);
    Task<string?> GetRolNombreAsync(int idRol, CancellationToken ct = default);
    Task<string?> GetEstadoCuentaNombreAsync(int idEstadoCuenta, CancellationToken ct = default);
    // PB-25 HU1-PB25: gestión de usuarios (excluye admins; filtros q/rol/estado).
    Task<List<UsuarioAdminRow>> GetUsuariosAsync(int idRolAdmin, string? q, int? idRol, int? idEstadoCuenta, CancellationToken ct = default);
    // PB-especialidades: reemplaza las especialidades del psicólogo (vacío = ninguna, verificadas).
    Task GuardarEspecialidadesAsync(int idPsicologo, List<int> idsEspecialidad, CancellationToken ct = default);
    // PB-25: reemplaza conservando verificación; las agregadas quedan ocultas (false).
    // Devuelve true si hubo cambios (agregadas o quitadas).
    Task<bool> ActualizarEspecialidadesAsync(int idPsicologo, List<int> idsEspecialidad, CancellationToken ct = default);
    Task SetTarjetaVerificadaAsync(int idPsicologo, bool verificada, CancellationToken ct = default);
    Task<List<string>> GetEspecialidadesPendientesAsync(int idPsicologo, CancellationToken ct = default);
    // PB-25: verifica las pendientes del psicólogo. Devuelve cuántas verificó.
    Task<int> VerificarEspecialidadesAsync(int idPsicologo, CancellationToken ct = default);
    // PB-25: psicólogos con tarjeta sin verificar o especialidades pendientes (cola del admin).
    Task<List<PsicologoTarjetaPendienteRow>> GetPsicologosTarjetaPendienteAsync(int idRolPsicologo, CancellationToken ct = default);

    // PB-26 HU1-PB26: validación de psicólogos (fiel a v1 CU26).
    Task<List<PsicologoPendienteRow>> GetPsicologosPendientesAsync(int idRolPsicologo, int idEstadoPendiente, CancellationToken ct = default);
    // Aprueba/rechaza solo si sigue pendiente (concurrencia). Devuelve true si aplicó.
    Task<bool> AprobarPsicologoAsync(int idPsicologo, int idEstadoPendiente, int idEstadoAprobado, int idEstadoActiva, CancellationToken ct = default);
    Task<bool> RechazarPsicologoAsync(int idPsicologo, int idEstadoPendiente, int idEstadoRechazado, int idEstadoInactiva, CancellationToken ct = default);
    Task SetEstadoAprobacionAsync(int idPsicologo, int idEstadoAprobacion, CancellationToken ct = default);
    Task<List<PsicologoDirectorioRow>> GetDirectorioPsicologosAsync(int idRolPsicologo, int idEstadoActiva, int idEstadoAprobado, int idEstadoAutorizado, string? q, int? idEspecialidad, CancellationToken ct = default);

    // PB-24 HU1-PB24: perfil profesional del psicólogo.
    Task<UsuarioDto?> FindPerfilProfesionalAsync(int idUsuario, CancellationToken ct = default);
    // PB-24/PB-25: actualiza perfil; especialidades con diff (agregadas ocultas,
    // tarjeta a no verificada si cambiaron). Devuelve true si hubo cambios.
    // La verificación en true solo la pone el admin.
    Task<bool> ActualizarPerfilProfesionalAsync(int idUsuario, int? añosExperiencia, string? descripcionProfesional, string? datosContacto, List<int> idsEspecialidad, CancellationToken ct = default);
}

// PB-especialidades: directorio para pacientes (nombre, especialidades, pacientes asignados).
public record PsicologoDirectorioRow(int Id, string Nombre, List<string> Especialidades, int PacientesAsignados);

// PB-25 HU1-PB25: fila de gestión de usuarios (sin admins).
public record UsuarioAdminRow(int Id, string Nombre, string Apellido, string Correo, int IdRol, string Rol, int IdEstadoCuenta, string Estado, int? IdEstadoAprobacion, string? Aprobacion, DateTime FechaRegistro);

// PB-25: cola del admin para verificar tarjetas/especialidades.
public record PsicologoTarjetaPendienteRow(int Id, string Nombre, string Correo, bool TarjetaVerificada, int Pendientes);

// PB-26 HU1-PB26: solicitudes pendientes de validación.
public record PsicologoPendienteRow(int Id, string Nombre, string Correo, DateTime FechaSolicitud);

public interface ICatalogRepository
{
    Task<List<CatalogItemDto>> GetTiposCuentaAsync(CancellationToken ct = default);
    Task<List<CatalogItemDto>> GetGenerosAsync(CancellationToken ct = default);
    Task<List<CatalogItemDto>> GetEspecialidadesAsync(CancellationToken ct = default);
    Task<int> GetRolIdAsync(string nombre, CancellationToken ct = default);
    Task<int> GetEstadoIdAsync(string nombre, CancellationToken ct = default);
    Task<bool> ExisteGeneroAsync(int idGenero, CancellationToken ct = default);
    Task<bool> ExisteEspecialidadAsync(int idEspecialidad, CancellationToken ct = default);
    Task<string?> GetRolNombreAsync(int idRol, CancellationToken ct = default);
    Task<string?> GetEstadoNombreAsync(int idEstado, CancellationToken ct = default);
}

public interface ICodigoRepository
{
    Task CrearAsync(int idUsuario, string codigo, string tipo, DateTime expiraEn, CancellationToken ct = default);
    Task< mindzyxsCode?> UltimoAsync(int idUsuario, string tipo, CancellationToken ct = default);
    Task ConsumirAsync(int idCodigo, CancellationToken ct = default);
}

public interface ISesionRepository
{
    Task<int> CrearAsync(int idUsuario, string tokenHash, DateTime expiraEn, CancellationToken ct = default);
    Task<SesionDto?> FindByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task DeleteAsync(int idSesion, CancellationToken ct = default);
    Task DeleteByTokenHashAsync(string tokenHash, CancellationToken ct = default);
    Task DeleteByUsuarioAsync(int idUsuario, CancellationToken ct = default);
}

public interface IAuthLogRepository
{
    Task LogAsync(int? actorId, int? targetId, string accion, string detalle, string? ip, CancellationToken ct = default);
}

// PB-13: historial emocional del paciente (eventos + respuestas + observaciones).
public interface IHistorialRepository
{
    Task<List<EventoRow>> GetEventosAsync(int idPaciente, DateTime? desde, DateTime? hasta, CancellationToken ct = default);
    Task<EventoRow?> FindEventoAsync(int idEvento, CancellationToken ct = default);
    Task<List<RespuestaRow>> GetRespuestasAsync(int idEvento, CancellationToken ct = default);
    Task<Dictionary<(int IdPregunta, int Valor), string>> GetTextosOpcionAsync(List<int> idsPregunta, CancellationToken ct = default);
    Task<List<ObservacionRow>> GetObservacionesAsync(int idEvento, CancellationToken ct = default);
    Task<Dictionary<int, string>> GetPreguntasTextoAsync(List<int> idsPregunta, CancellationToken ct = default);
    Task<Dictionary<int, string>> GetNivelesAsync(CancellationToken ct = default);
    Task<Dictionary<int, string>> GetCuestionariosTituloAsync(List<int> ids, CancellationToken ct = default);
    Task<List<UsuarioDto>> GetPacientesAutorizadosAsync(int idPsicologo, int idEstadoAutorizado, CancellationToken ct = default);
    Task<List<int>> GetPsicologosAutorizadosAsync(int idPaciente, int idEstadoAutorizado, CancellationToken ct = default);
    Task<bool> EstaAutorizadoAsync(int idPaciente, int idPsicologo, int idEstadoAutorizado, CancellationToken ct = default);
    Task AsegurarAutorizacionAsync(int idPaciente, int idPsicologo, int idEstadoAutorizado, CancellationToken ct = default);
    Task RevocarAutorizacionAsync(int idPaciente, int idPsicologo, int idEstadoRevocado, CancellationToken ct = default);
    Task<int?> GetEstadoIdAsync(string nombre, CancellationToken ct = default);

    // PB-11 HU1-PB11: escritura del resultado de un cuestionario en el historial del
    // paciente (eventos_bienestar + respuestas, en una sola transacción).
    Task<List<int>> GetCuestionariosRespondidosAsync(int idPaciente, CancellationToken ct = default);
    Task<int?> GetNivelIdAsync(string nombre, CancellationToken ct = default);
    Task<int?> GetTipoEventoIdAsync(string nombre, CancellationToken ct = default);
    Task<int> GuardarEventoCuestionarioAsync(int idPaciente, int idCuestionario, int idNivelRiesgo, int puntajeTotal, List<RespuestaRow> respuestas, CancellationToken ct = default);

    // PB-12 HU1-PB12: guarda el registro del estado emocional del paciente
    // (evento tipo RegistroEmocional) con fecha/hora automática y nota opcional.
    Task<int> GuardarEstadoEmocionalAsync(int idPaciente, int idTipoEvento, int idNivelRiesgo, int puntaje, string? nota, DateTime fechaEvento, CancellationToken ct = default);

    // PB-19 HU1-PB19: guarda la observación del psicólogo sobre un paciente autorizado
    // (opcionalmente ligada a un evento). Devuelve el id de la observación.
    Task<int> GuardarObservacionAsync(int idPsicologo, int idPaciente, int? idEvento, string contenido, string firma, CancellationToken ct = default);
}

// PB-15: recordatorios del paciente (una config por paciente + días).
public interface IRecordatorioRepository
{
    Task<RecordatorioRow?> GetDePacienteAsync(int idPaciente, CancellationToken ct = default);
    Task<List<int>> GetDiasAsync(int idRecordatorio, CancellationToken ct = default);
    Task<RecordatorioRow> GuardarAsync(int idPaciente, string titulo, string? mensaje, TimeSpan hora, string frecuencia, bool activo, List<int> dias, CancellationToken ct = default);
}

// PB-07: lectura de cuestionarios publicados para invitados (y pacientes).
// Solo Predeterminado + Publicado; los Personalizado van por asignaciones (PB-22).
public record AsignacionCuestionarioDto(int IdAsignacion, int IdCuestionario, string TituloCuestionario, string TipoCuestionario, int IdPaciente, int IdPsicologo, DateTime FechaAsignacion, DateTime? FechaLimite, string Estado);

public interface ICuestionarioRepository
{
    Task<List<CuestionarioRow>> GetPublicadosInvitadoAsync(CancellationToken ct = default);
    Task<CuestionarioRow?> FindPublicadoInvitadoAsync(int idCuestionario, CancellationToken ct = default);
    Task<(List<PreguntaRow> Preguntas, List<OpcionPreguntaRow> Opciones)> GetCompletoAsync(int idCuestionario, CancellationToken ct = default);
    Task<List<AsignacionCuestionarioDto>> GetAsignadosPorPacienteAsync(int idPaciente, CancellationToken ct = default);
    Task<AsignacionCuestionarioDto?> FindAsignacionPendienteAsync(int idPaciente, int idCuestionario, CancellationToken ct = default);
    Task MarcarAsignacionCompletadaAsync(int idAsignacion, CancellationToken ct = default);
    Task<CuestionarioRow?> FindByIdAsync(int idCuestionario, CancellationToken ct = default);

    // PB-22 HU1-PB22: ids de pacientes con asignación Pendiente/Asignado de un
    // cuestionario, para prellenar la pantalla (deshabilitar ya asignados).
    Task<List<int>> GetPacientesConAsignacionPendienteAsync(int idCuestionario, CancellationToken ct = default);

    // PB-22 HU1-PB22: el psicólogo asigna un cuestionario propio Publicado a los
    // pacientes autorizados indicados (una fila por paciente). Devuelve los ids de
    // las asignaciones creadas.
    Task<List<int>> AsignarAsync(int idCuestionario, int idPsicologo, List<int> idsPacientes, CancellationToken ct = default);

    // PB-28 HU1-PB28: admin gestiona cuestionarios Predeterminado (lista, crear,
    // editar, desactivar). Solo tipo Predeterminado; estado Inactivo = desactivado.
    Task<List<CuestionarioRow>> GetPredeterminadosAsync(string? q, CancellationToken ct = default);
    Task<CuestionarioRow?> FindPredeterminadoAsync(int idCuestionario, CancellationToken ct = default);
    Task<int> CrearPredeterminadoAsync(CuestionarioRow cuestionario, List<(PreguntaRow Pregunta, List<OpcionPreguntaRow> Opciones)> preguntas, CancellationToken ct = default);
    Task<int> ActualizarPredeterminadoAsync(int idCuestionario, string titulo, string? descripcion, string estado, string? modoCalculo, int? umbralBajoMax, int? umbralMedioMax, List<(PreguntaRow Pregunta, List<OpcionPreguntaRow> Opciones)> preguntas, CancellationToken ct = default);
    Task DesactivarAsync(int idCuestionario, CancellationToken ct = default);

    // PB-20 HU1-PB20: crea el cuestionario Personalizado con sus preguntas y opciones
    // en una transacción. Devuelve el id del cuestionario.
    Task<int> CrearAsync(CuestionarioRow cuestionario, List<(PreguntaRow Pregunta, List<OpcionPreguntaRow> Opciones)> preguntas, CancellationToken ct = default);

    // PB-21 HU1-PB21: gestión de los cuestionarios del psicólogo (listar, editar,
    // despublicar y eliminar). La eliminación es lógica (Estado = "Eliminado") porque
    // eventos/respuestas apuntan a la fila por FK; lo mismo con preguntas en uso
    // (marca `eliminada`), mientras las opciones sí se reconstruyen libremente.
    Task<List<CuestionarioPsicologoListaDto>> GetDePsicologoAsync(int idPsicologo, string? q, CancellationToken ct = default);
    Task<CuestionarioRow?> FindDePsicologoAsync(int idCuestionario, int idPsicologo, CancellationToken ct = default);
    Task<int> ActualizarAsync(int idCuestionario, string titulo, string? descripcion, string estado, string? modoCalculo, int? umbralBajoMax, int? umbralMedioMax, List<(PreguntaRow Pregunta, List<OpcionPreguntaRow> Opciones)> preguntas, CancellationToken ct = default);
    Task DespublicarAsync(int idCuestionario, CancellationToken ct = default);
    Task EliminarAsync(int idCuestionario, CancellationToken ct = default);
    Task EliminarObservacionesDeCuestionarioAsync(int idCuestionario, CancellationToken ct = default);
}

// PB-08: líneas de ayuda activas para invitados y pacientes (las administra el admin, PB-29).
public interface ILineaAyudaRepository
{
    Task<List<LineaAyudaRow>> GetActivasAsync(CancellationToken ct = default);

    // PB-29 HU1-PB29: admin gestiona líneas de ayuda (lista, crear, editar, activar/inactivar, eliminar).
    Task<List<LineaAyudaRow>> GetAllAsync(CancellationToken ct = default);
    Task<LineaAyudaRow?> FindByIdAsync(int idLinea, CancellationToken ct = default);
    Task<int> CrearAsync(LineaAyudaRow linea, CancellationToken ct = default);
    Task ActualizarAsync(int idLinea, string nombre, string? descripcion, string? telefono, string? url, string? horario, string? cobertura, string? descripcionCorta, string? whatsapp, string? email, string? chatUrl, string? videollamada, string? tipoAtencion, string? departamento, string? municipio, string? poblacion, string? fuente, DateOnly? fechaVerificacion, CancellationToken ct = default);
    Task ActivarAsync(int idLinea, CancellationToken ct = default);
    Task InactivarAsync(int idLinea, CancellationToken ct = default);
    Task EliminarAsync(int idLinea, CancellationToken ct = default);
}

// PB-09: recomendaciones básicas de autocuidado (contenido administrable, PB-28).
public interface IRecomendacionRepository
{
    Task<List<RecomendacionRow>> GetPriorizadasAsync(string? nivel, CancellationToken ct = default);
    Task<RecomendacionRow?> FindByIdAsync(int idRecomendacion, CancellationToken ct = default);
}

// PB-31: ciclo de vida de conversaciones en SQL (los mensajes van por RTDB).
public interface IChatRepository
{
    Task<List<ConversacionRow>> GetDeUsuarioAsync(int idUsuario, CancellationToken ct = default);
    Task<ConversacionRow?> FindAsync(int idConversacion, CancellationToken ct = default);
    Task<ConversacionRow?> FindParejaAsync(int idPaciente, int idPsicologo, CancellationToken ct = default);
    Task<ConversacionRow> CrearAsync(int idPaciente, int idPsicologo, int idSolicitante, CancellationToken ct = default);
    Task SetEstadoAsync(int idConversacion, string estado, int? idBloqueador, CancellationToken ct = default);
    Task SolicitarBloqueoAsync(int idConversacion, string motivo, CancellationToken ct = default);
    Task<List<PacienteBusquedaDto>> BuscarPacientesAsync(int idRolPaciente, int idEstadoActiva, string? q, string? nivel, int? minCuestionarios, bool? tienePsicologo, CancellationToken ct = default);
}

public record PacienteBusquedaDto(int Id, string Nombre, string Correo, int? UltimoPuntaje, string? UltimoNivel, DateTime? UltimaFecha, int CuestionariosRespondidos, int Psicologos);

// PB-30: catálogo de notificaciones del admin.
public interface INotificacionRepository
{
    Task<List<NotificacionRow>> GetAllAsync(CancellationToken ct = default);
    Task<NotificacionRow?> FindByTipoAsync(string tipo, CancellationToken ct = default);
    Task GuardarAsync(string tipo, string canales, bool activo, string? asunto, string? cuerpo, int idAdmin, CancellationToken ct = default);
}

public interface IAdminRepository
{
    Task<List<RespaldoDto>> ListRespaldosAsync(CancellationToken ct = default);
    Task<int> CrearRespaldoAsync(string nombreArchivo, string tamano, int idAdmin, CancellationToken ct = default);
    Task<RespaldoDto?> FindRespaldoByIdAsync(int idRespaldo, CancellationToken ct = default);
    Task<MantenimientoLimpiezaResult> LimpiarTodoAsync(int dias, CancellationToken ct = default);
    Task<string> ExportSnapshotJsonAsync(CancellationToken ct = default);
    Task CreateSqlBackupAsync(string rutaArchivo, CancellationToken ct = default);
}

public record MantenimientoLimpiezaResult(int Codigos, int Sesiones, int Auth);

public record RespaldoDto(int IdRespaldo, string NombreArchivo, string? Tamano, DateTime FechaHora, int IdAdmin);
public record mindzyxsCode(int IdCodigo, string Codigo, DateTime ExpiraEn);
public record SesionDto(int IdSesion, int IdUsuario, string TokenHash, DateTime ExpiraEn, DateTime CreadaEn);
