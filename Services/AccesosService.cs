using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class AccesosService(
    IUserRepository users,
    ICatalogRepository catalogs,
    ISesionRepository sesiones,
    IHistorialRepository historial,
    IAuthLogRepository authLogs)
{
    // PB-17 HU1-PB17: psicólogos aprobados+activos con estado de autorización del paciente.
    public async Task<List<AccesoPsicologoDto>> MisAccesosAsync(string? tokenRaw, string? q, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePacienteAsync(tokenRaw, ct);
        var idRolPsi = await catalogs.GetRolIdAsync(AppRoles.Psicologo, ct);
        var idActiva = await catalogs.GetEstadoIdAsync(AccountStates.Activa, ct);
        var idAprob = await catalogs.GetEstadoIdAsync(AccountStates.Aprobado, ct);
        var idAut = await historial.GetEstadoIdAsync("Autorizado", ct) ?? -1;
        var lista = await users.GetDirectorioPsicologosAsync(idRolPsi, idActiva, idAprob, idAut, q, null, ct);
        var setAut = (await historial.GetPsicologosAutorizadosAsync(yo.IdUsuario, idAut, ct)).ToHashSet();

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.DirectorioConsulta,
            $"Paciente:{yo.IdUsuario} consultó su gestión de accesos ({lista.Count})", ip, ct);

        return lista.Select(p => new AccesoPsicologoDto
        {
            IdPsicologo = p.Id,
            Nombre = p.Nombre,
            Especialidades = p.Especialidades,
            Autorizado = setAut.Contains(p.Id)
        }).ToList();
    }

    // PB-17 HU1-PB17: autoriza a un psicólogo aprobado y activo.
    public async Task<string> AutorizarAsync(string? tokenRaw, int idPsicologo, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePacienteAsync(tokenRaw, ct);
        await ValidarPsicologoAutorizableAsync(idPsicologo, ct);
        var idAut = await historial.GetEstadoIdAsync("Autorizado", ct)
            ?? throw new InvalidOperationException("Estado Autorizado no configurado.");
        await historial.AsegurarAutorizacionAsync(yo.IdUsuario, idPsicologo, idAut, ct);
        await authLogs.LogAsync(yo.IdUsuario, idPsicologo, LogAcciones.AutorizacionOtorgada,
            $"Paciente:{yo.IdUsuario} autorizó al psicólogo:{idPsicologo} desde gestión de accesos", ip, ct);
        return "Acceso autorizado correctamente";
    }

    // PB-17 HU1-PB17: revoca el acceso de un psicólogo.
    public async Task<string> RevocarAsync(string? tokenRaw, int idPsicologo, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePacienteAsync(tokenRaw, ct);
        var psi = await users.FindByIdAsync(idPsicologo, ct);
        if (psi is null)
            throw new InvalidOperationException("Psicólogo no encontrado.");
        var idRev = await historial.GetEstadoIdAsync("Revocado", ct)
            ?? throw new InvalidOperationException("Estado Revocado no configurado.");
        await historial.RevocarAutorizacionAsync(yo.IdUsuario, idPsicologo, idRev, ct);
        await authLogs.LogAsync(yo.IdUsuario, idPsicologo, LogAcciones.AutorizacionRevocada,
            $"Paciente:{yo.IdUsuario} revocó al psicólogo:{idPsicologo} desde gestión de accesos", ip, ct);
        return "Acceso revocado correctamente";
    }

    // Restringe a psicólogos con rol Psicologo, cuenta Activa y aprobación Aprobado.
    private async Task ValidarPsicologoAutorizableAsync(int idPsicologo, CancellationToken ct)
    {
        var psi = await users.FindByIdAsync(idPsicologo, ct);
        if (psi is null)
            throw new InvalidOperationException("Psicólogo no encontrado.");
        var rol = await users.GetRolNombreAsync(psi.IdRol, ct);
        if (!string.Equals(rol, AppRoles.Psicologo, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Psicólogo no válido.");
        var estado = await users.GetEstadoCuentaNombreAsync(psi.IdEstadoCuenta, ct);
        if (!string.Equals(estado, AccountStates.Activa, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El psicólogo no está activo.");
        if (psi.IdEstadoAprobacion.HasValue)
        {
            var aprob = await catalogs.GetEstadoNombreAsync(psi.IdEstadoAprobacion.Value, ct);
            if (!string.Equals(aprob, AccountStates.Aprobado, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("El psicólogo aún no está aprobado.");
        }
    }

    // Requiere sesión de paciente.
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
