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

    // PB-24 HU1-PB24: obtiene el perfil profesional del psicólogo autenticado.
    public async Task<PerfilProfesionalVm> ObtenerPerfilProfesionalAsync(string? tokenRaw, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);
        var perfil = await users.FindPerfilProfesionalAsync(yo.IdUsuario, ct)
            ?? throw new InvalidOperationException("Perfil profesional no encontrado.");

        return new PerfilProfesionalVm
        {
            Id = perfil.IdUsuario,
            Nombre = $"{perfil.Nombre} {perfil.Apellido}".Trim(),
            Correo = perfil.Correo,
            AñosExperiencia = perfil.AñosExperiencia,
            DescripcionProfesional = perfil.DescripcionProfesional,
            DatosContacto = perfil.DatosContacto,
            TarjetaProfesionalVerificada = perfil.TarjetaProfesionalVerificada,
            Especialidades = perfil.Especialidades,
            TieneTarjetaArchivo = !string.IsNullOrWhiteSpace(perfil.FilePath)
        };
    }

    // PB-24 HU1-PB24: actualiza el perfil profesional del psicólogo autenticado.
    public async Task<string> ActualizarPerfilProfesionalAsync(string? tokenRaw, ActualizarPerfilProfesionalVm vm, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);

        // Validaciones
        if (vm.AñosExperiencia.HasValue && vm.AñosExperiencia.Value < 0)
            throw new InvalidOperationException("Los años de experiencia no pueden ser negativos");

        if (vm.Correo is not null)
        {
            var correo = ContactHelper.Normalize(vm.Correo);
            if (!ContactHelper.IsEmail(correo))
                throw new InvalidOperationException("Ingresa un correo válido");
            if (await users.ExistsByCorreoAsync(correo, ct) && !string.Equals(correo, yo.Correo, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("El correo ya se encuentra registrado");
        }

        var idsEspecialidad = (vm.Especialidades ?? []).Distinct().ToList();
        foreach (var id in idsEspecialidad)
        {
            if (!await catalogs.ExisteEspecialidadAsync(id, ct))
                throw new InvalidOperationException($"La especialidad {id} no existe");
        }

        // Actualizar perfil profesional
        await users.ActualizarPerfilProfesionalAsync(
            yo.IdUsuario,
            vm.AñosExperiencia,
            vm.DescripcionProfesional,
            vm.DatosContacto,
            vm.TarjetaProfesionalVerificada ?? false,
            idsEspecialidad,
            ct);

        // Si se proporciona correo nuevo y es diferente, actualizarlo
        if (vm.Correo is not null)
        {
            var correo = ContactHelper.Normalize(vm.Correo);
            if (!string.Equals(correo, yo.Correo, StringComparison.OrdinalIgnoreCase))
            {
                await users.ActualizarCorreoAsync(yo.IdUsuario, correo, ct);
            }
        }

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.PerfilProfesionalActualizado,
            $"Psicólogo:{yo.IdUsuario} actualizó su perfil profesional", ip, ct);

        return "Información profesional actualizada correctamente";
    }

    // Requiere sesión de psicólogo activo y aprobado.
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