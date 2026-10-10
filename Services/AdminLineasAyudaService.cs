using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class AdminLineasAyudaService(
    ILineaAyudaRepository lineas,
    IUserRepository users,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs)
{
    // PB-29 HU1-PB29: lista todas las líneas de ayuda (admin, con búsqueda).
    // Solo administrador activo.
    public async Task<List<LineaAyudaAdminListaDto>> ListarAsync(string? tokenRaw, string? q, CancellationToken ct = default)
    {
        await RequireAdminAsync(tokenRaw, ct);
        var busqueda = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
        var lista = await lineas.GetAllAsync(ct);
        return lista.Where(l => string.IsNullOrEmpty(busqueda) || l.Nombre.Contains(busqueda, StringComparison.OrdinalIgnoreCase))
            .Select(l => new LineaAyudaAdminListaDto
            {
                Id = l.IdLinea,
                Nombre = l.Nombre,
                Telefono = l.Telefono,
                Url = l.Url,
                Estado = l.Estado
            }).ToList();
    }

    // PB-29: detalle de una línea de ayuda para editar.
    public async Task<LineaAyudaAdminDetalleDto> ObtenerDetalleAsync(string? tokenRaw, int id, CancellationToken ct = default)
    {
        await RequireAdminAsync(tokenRaw, ct);
        var l = await lineas.FindByIdAsync(id, ct)
            ?? throw new InvalidOperationException("Línea de ayuda no encontrada.");

        return new LineaAyudaAdminDetalleDto
        {
            Id = l.IdLinea,
            Nombre = l.Nombre,
            Descripcion = l.Descripcion,
            Telefono = l.Telefono,
            Url = l.Url,
            Horario = l.Horario,
            Cobertura = l.Cobertura,
            DescripcionCorta = l.Descripcion,
            Whatsapp = l.Whatsapp,
            Email = l.Email,
            ChatUrl = l.ChatUrl,
            Videollamada = l.Videollamada,
            TipoAtencion = l.TipoAtencion,
            Departamento = l.Departamento,
            Municipio = l.Municipio,
            Poblacion = l.Poblacion,
            Fuente = l.Fuente,
            FechaVerificacion = l.FechaVerificacion,
            Estado = l.Estado
        };
    }

    // PB-29 HU1-PB29: crea una nueva línea de ayuda (valida nombre y al menos un contacto).
    // Estado inicial: Activa. Log LINEA_AYUDA_CREADA.
    public async Task<LineaAyudaAdminResultadoDto> CrearAsync(string? tokenRaw, LineaAyudaAdminCrearDto dto, string? ip, CancellationToken ct = default)
    {
        await RequireAdminAsync(tokenRaw, ct);

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new InvalidOperationException("Debes ingresar nombre y al menos un medio de contacto (teléfono o URL)");
        if (string.IsNullOrWhiteSpace(dto.Telefono) && string.IsNullOrWhiteSpace(dto.Url))
            throw new InvalidOperationException("Debes ingresar nombre y al menos un medio de contacto (teléfono o URL)");

        var linea = new LineaAyudaRow
        {
            Nombre = dto.Nombre.Trim(),
            Descripcion = dto.Descripcion?.Trim(),
            Telefono = dto.Telefono?.Trim(),
            Url = dto.Url?.Trim(),
            Horario = dto.Horario?.Trim(),
            Cobertura = dto.Cobertura?.Trim(),
            Whatsapp = dto.Whatsapp?.Trim(),
            Email = dto.Email?.Trim(),
            ChatUrl = dto.ChatUrl?.Trim(),
            Videollamada = dto.Videollamada?.Trim(),
            TipoAtencion = dto.TipoAtencion?.Trim(),
            Departamento = dto.Departamento?.Trim(),
            Municipio = dto.Municipio?.Trim(),
            Poblacion = dto.Poblacion?.Trim(),
            Fuente = dto.Fuente?.Trim(),
            FechaVerificacion = dto.FechaVerificacion,
            Estado = "Activa"
        };

        var id = await lineas.CrearAsync(linea, ct);

        await authLogs.LogAsync(null, null, LogAcciones.LineaAyudaCreada,
            $"Admin creó la línea de ayuda {id} \"{linea.Nombre}\" (Activa)", ip, ct);

        return new LineaAyudaAdminResultadoDto { Id = id, Mensaje = "Línea de ayuda creada correctamente" };
    }

    // PB-29: edita una línea existente.
    // Valida nombre y al menos un contacto. Log LINEA_AYUDA_EDITADA.
    public async Task<LineaAyudaAdminResultadoDto> ActualizarAsync(string? tokenRaw, int id, LineaAyudaAdminCrearDto dto, string? ip, CancellationToken ct = default)
    {
        await RequireAdminAsync(tokenRaw, ct);
        var l = await lineas.FindByIdAsync(id, ct)
            ?? throw new InvalidOperationException("Línea de ayuda no encontrada.");

        if (string.IsNullOrWhiteSpace(dto.Nombre))
            throw new InvalidOperationException("Debes ingresar nombre y al menos un medio de contacto (teléfono o URL)");
        if (string.IsNullOrWhiteSpace(dto.Telefono) && string.IsNullOrWhiteSpace(dto.Url))
            throw new InvalidOperationException("Debes ingresar nombre y al menos un medio de contacto (teléfono o URL)");

        await lineas.ActualizarAsync(
            id,
            dto.Nombre.Trim(),
            dto.Descripcion?.Trim(),
            dto.Telefono?.Trim(),
            dto.Url?.Trim(),
            dto.Horario?.Trim(),
            dto.Cobertura?.Trim(),
            dto.DescripcionCorta?.Trim(),
            dto.Whatsapp?.Trim(),
            dto.Email?.Trim(),
            dto.ChatUrl?.Trim(),
            dto.Videollamada?.Trim(),
            dto.TipoAtencion?.Trim(),
            dto.Departamento?.Trim(),
            dto.Municipio?.Trim(),
            dto.Poblacion?.Trim(),
            dto.Fuente?.Trim(),
            dto.FechaVerificacion,
            default);

        await authLogs.LogAsync(null, null, LogAcciones.LineaAyudaEditada,
            $"Admin editó la línea de ayuda {id} \"{dto.Nombre.Trim()}\"", ip, ct);

        return new LineaAyudaAdminResultadoDto { Id = id, Mensaje = "Línea de ayuda actualizada correctamente" };
    }

    // PB-29: activa una línea (estado = Activa). Log LINEA_AYUDA_ACTIVADA.
    public async Task<string> ActivarAsync(string? tokenRaw, int id, string? ip, CancellationToken ct = default)
    {
        await RequireAdminAsync(tokenRaw, ct);
        var l = await lineas.FindByIdAsync(id, ct)
            ?? throw new InvalidOperationException("Línea de ayuda no encontrada.");

        await lineas.ActivarAsync(id, ct);
        await authLogs.LogAsync(null, null, LogAcciones.LineaAyudaActivada,
            $"Admin activó la línea de ayuda {id} \"{l.Nombre}\"", ip, ct);
        return "Línea de ayuda activada correctamente";
    }

    // PB-29: inactiva una línea (estado = Inactiva). Log LINEA_AYUDA_INACTIVADA.
    public async Task<string> InactivarAsync(string? tokenRaw, int id, string? ip, CancellationToken ct = default)
    {
        await RequireAdminAsync(tokenRaw, ct);
        var l = await lineas.FindByIdAsync(id, ct)
            ?? throw new InvalidOperationException("Línea de ayuda no encontrada.");

        await lineas.InactivarAsync(id, ct);
        await authLogs.LogAsync(null, null, LogAcciones.LineaAyudaInactivada,
            $"Admin inactivó la línea de ayuda {id} \"{l.Nombre}\"", ip, ct);
        return "Línea de ayuda inactivada correctamente";
    }

    // PB-29: elimina (borrado lógico -> estado Eliminada). Log LINEA_AYUDA_ELIMINADA.
    public async Task<string> EliminarAsync(string? tokenRaw, int id, string? ip, CancellationToken ct = default)
    {
        await RequireAdminAsync(tokenRaw, ct);
        var l = await lineas.FindByIdAsync(id, ct)
            ?? throw new InvalidOperationException("Línea de ayuda no encontrada.");

        await lineas.EliminarAsync(id, ct);
        await authLogs.LogAsync(null, null, LogAcciones.LineaAyudaEliminada,
            $"Admin eliminó la línea de ayuda {id} \"{l.Nombre}\" (borrado lógico)", ip, ct);
        return "Línea de ayuda eliminada correctamente";
    }

    // Requiere administrador con sesión activa.
    private async Task<int?> RequireAdminAsync(string? tokenRaw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenRaw))
            throw new UnauthorizedAccessException("Tu sesión ha expirado. Inicia sesión nuevamente");
        var sesion = await sesiones.FindByTokenHashAsync(TokenHelper.Hash(tokenRaw.Trim()), ct);
        if (sesion is null || sesion.ExpiraEn < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Tu sesión ha expirado. Inicia sesión nuevamente");
        var user = await users.FindByIdAsync(sesion.IdUsuario, ct)
            ?? throw new UnauthorizedAccessException("Tu sesión ha expirado. Inicia sesión nuevamente");
        var rol = await users.GetRolNombreAsync(user.IdRol, ct);
        if (!string.Equals(rol, AppRoles.Administrador, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("No tienes permisos para acceder a esta función");
        return user.IdUsuario;
    }
}