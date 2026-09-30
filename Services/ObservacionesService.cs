using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class ObservacionesService(
    IUserRepository users,
    ICatalogRepository catalogs,
    ISesionRepository sesiones,
    IHistorialRepository historial,
    IAuthLogRepository authLogs)
{
    // PB-19 HU1-PB19: guarda la observación sobre un paciente autorizado
    // (opcionalmente ligada a un registro). Firma y fecha/hora automáticas.
    public async Task<string> CrearAsync(string? tokenRaw, ObservacionCrearDto dto, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);
        if (string.IsNullOrWhiteSpace(dto.Contenido))
            throw new InvalidOperationException("La observación es obligatoria.");

        var paciente = await users.FindByIdAsync(dto.IdPaciente, ct);
        if (paciente is null)
            throw new KeyNotFoundException("Paciente no encontrado.");

        var idAut = await historial.GetEstadoIdAsync("Autorizado", ct) ?? -1;
        if (!await historial.EstaAutorizadoAsync(dto.IdPaciente, yo.IdUsuario, idAut, ct))
            throw new InvalidOperationException("No tienes autorización para agregar observaciones a este paciente.");

        int? idEvento = null;
        if (dto.IdEvento.HasValue)
        {
            var evento = await historial.FindEventoAsync(dto.IdEvento.Value, ct);
            if (evento is null || evento.IdPaciente != dto.IdPaciente)
                throw new KeyNotFoundException("Registro no encontrado.");
            idEvento = evento.IdEvento;
        }

        var firma = $"Psicólogo {yo.Nombre} {yo.Apellido}".Trim();
        var idObs = await historial.GuardarObservacionAsync(
            yo.IdUsuario, dto.IdPaciente, idEvento, dto.Contenido.Trim(), firma, ct);

        await authLogs.LogAsync(yo.IdUsuario, dto.IdPaciente, LogAcciones.ObservacionCrear,
            $"Psicólogo:{yo.IdUsuario} guardó la observación {idObs} del paciente:{dto.IdPaciente}" +
            (idEvento.HasValue ? $" (registro {idEvento.Value})" : ""), ip, ct);

        return "Observación guardada correctamente";
    }

    // Requiere psicólogo con cuenta activa y aprobada.
    private async Task<UsuarioDto> RequirePsicologoAsync(string? tokenRaw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenRaw))
            throw new UnauthorizedAccessException("Sesión requerida");
        var sesion = await sesiones.FindByTokenHashAsync(TokenHelper.Hash(tokenRaw.Trim()), ct);
        if (sesion is null || sesion.ExpiraEn < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Sesión requerida");
        var user = await users.FindByIdAsync(sesion.IdUsuario, ct)
            ?? throw new UnauthorizedAccessException("Sesión requerida");
        var rol = await users.GetRolNombreAsync(user.IdRol, ct);
        if (!string.Equals(rol, AppRoles.Psicologo, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("No tienes permisos para acceder a esta función");
        var estado = await users.GetEstadoCuentaNombreAsync(user.IdEstadoCuenta, ct);
        if (!string.Equals(estado, AccountStates.Activa, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La cuenta no está activa.");
        if (user.IdEstadoAprobacion.HasValue)
        {
            var aprob = await catalogs.GetEstadoNombreAsync(user.IdEstadoAprobacion.Value, ct);
            if (!string.Equals(aprob, AccountStates.Aprobado, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("El psicólogo aún no está aprobado.");
        }
        return user;
    }
}
