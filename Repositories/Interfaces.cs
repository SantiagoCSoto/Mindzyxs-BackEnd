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
    // PB-especialidades: reemplaza las especialidades del psicólogo (vacío = ninguna).
    Task GuardarEspecialidadesAsync(int idPsicologo, List<int> idsEspecialidad, CancellationToken ct = default);
    Task<List<PsicologoDirectorioRow>> GetDirectorioPsicologosAsync(int idRolPsicologo, int idEstadoActiva, int idEstadoAprobado, int idEstadoAutorizado, string? q, int? idEspecialidad, CancellationToken ct = default);
}

// PB-especialidades: directorio para pacientes (nombre, especialidades, pacientes asignados).
public record PsicologoDirectorioRow(int Id, string Nombre, List<string> Especialidades, int PacientesAsignados);

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
}

// PB-08: líneas de ayuda activas para invitados y pacientes (las administra el admin, PB-29).
public interface ILineaAyudaRepository
{
    Task<List<LineaAyudaRow>> GetActivasAsync(CancellationToken ct = default);
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
