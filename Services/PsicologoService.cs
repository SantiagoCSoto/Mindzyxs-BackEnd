using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class PsicologoService(
    IUserRepository users,
    ICatalogRepository catalogs,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs)
{
    // Directorio para pacientes: psicólogos (con especialidades y pacientes asignados).
    // Filtros opcionales: q (nombre) y idEspecialidad (checklist de especialidades).
    public async Task<List<PsicologoDirectorioDto>> DirectorioAsync(string? tokenRaw, string? q, int? idEspecialidad, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePacienteAsync(tokenRaw, ct);
        var idRolPsi = await catalogs.GetRolIdAsync(AppRoles.Psicologo, ct);
        var idActiva = await catalogs.GetEstadoIdAsync(AccountStates.Activa, ct);
        var idAprob = await catalogs.GetEstadoIdAsync(AccountStates.Aprobado, ct);
        var idAut = await catalogs.GetEstadoIdAsync("Autorizado", ct);
        var lista = await users.GetDirectorioPsicologosAsync(idRolPsi, idActiva, idAprob, idAut, q, idEspecialidad, ct);

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.DirectorioConsulta,
            $"Paciente:{yo.IdUsuario} consultó el directorio de psicólogos ({lista.Count})", ip, ct);

        return lista.Select(p => new PsicologoDirectorioDto
        {
            Id = p.Id,
            Nombre = p.Nombre,
            Especialidades = p.Especialidades,
            PacientesAsignados = p.PacientesAsignados
        }).ToList();
    }

    // Requiere sesión de paciente (solo pacientes ven el directorio).
    private async Task<UsuarioDto> RequirePacienteAsync(string? tokenRaw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenRaw))
            throw new UnauthorizedAccessException("Sesión requerida");
        var sesion = await sesiones.FindByTokenHashAsync(TokenHelper.Hash(tokenRaw.Trim()), ct);
        if (sesion is null || sesion.ExpiraEn < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Sesión requerida");
        var user = await users.FindByIdAsync(sesion.IdUsuario, ct)
            ?? throw new UnauthorizedAccessException("Sesión requerida");
        var rol = await users.GetRolNombreAsync(user.IdRol, ct);
        if (!string.Equals(rol, AppRoles.Paciente, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("No tienes permisos para acceder a esta función");
        return user;
    }
}
