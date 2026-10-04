using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class ChatService(
    IChatRepository chats,
    IUserRepository usuarios,
    ICatalogRepository catalogos,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs,
    IHistorialRepository historial,
    FirebaseService firebase)
{
    // PB-31: mis conversaciones (paciente ve las suyas, psicólogo las suyas).
    public async Task<List<ConversacionDto>> MisConversacionesAsync(string? tokenRaw, string? ip, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        var rows = await chats.GetDeUsuarioAsync(yo.IdUsuario, ct);
        var list = new List<ConversacionDto>();
        foreach (var c in rows)
        {
            var otroId = c.IdPaciente == yo.IdUsuario ? c.IdPsicologo : c.IdPaciente;
            var otro = await usuarios.FindByIdAsync(otroId, ct);
            list.Add(new ConversacionDto
            {
                Id = c.IdConversacion,
                Estado = c.Estado,
                BloqueadaPorMi = c.IdBloqueador == yo.IdUsuario,
                IdSolicitante = c.IdSolicitante,
                MotivoBloqueo = c.MotivoBloqueo,
                Contraparte = new ChatContraparteDto
                {
                    Id = otroId,
                    Nombre = otro is null ? $"Usuario:{otroId}" : $"{otro.Nombre} {otro.Apellido}".Trim(),
                    Rol = otro is null ? "" : await catalogos.GetRolNombreAsync(otro.IdRol, ct) ?? ""
                },
                FechaCreacion = c.FechaCreacion
            });
        }
        return list;
    }

    // PB-31: el psicólogo (aprobado y activo) ofrece chat a un paciente activo.
    // Si hubo Rechazada/Cerrada se reactiva a Pendiente. Lanza InvalidOperationException.
    public async Task<ConversacionDto> CrearSolicitudAsync(string? tokenRaw, int idPaciente, string? ip, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        await RequireRolAsync(yo.IdUsuario, AppRoles.Psicologo, ct);
        await RequireAprobadoAsync(yo.IdUsuario, ct);

        var pac = await usuarios.FindByIdAsync(idPaciente, ct)
            ?? throw new InvalidOperationException("Paciente no encontrado.");
        await RequireRolAsync(idPaciente, AppRoles.Paciente, ct);
        await RequireActivaAsync(idPaciente, ct);

        var existente = await chats.FindParejaAsync(idPaciente, yo.IdUsuario, ct);
        ConversacionRow row;
        if (existente is null)
        {
            row = await chats.CrearAsync(idPaciente, yo.IdUsuario, yo.IdUsuario, ct);
        }
        else if (existente.Estado is "Rechazada" or "Cerrada")
        {
            await chats.SetEstadoAsync(existente.IdConversacion, "Pendiente", null, ct);
            row = (await chats.FindAsync(existente.IdConversacion, ct))!;
        }
        else
        {
            throw new InvalidOperationException("Ya existe una solicitud o conversación activa con este paciente.");
        }

        await authLogs.LogAsync(yo.IdUsuario, idPaciente, LogAcciones.ChatSolicitud,
            $"Psicólogo:{yo.IdUsuario} ofreció chat al paciente:{idPaciente} (conversación {row.IdConversacion})", ip, ct);
        return await ToDtoAsync(row.IdConversacion, yo.IdUsuario, ct);
    }

    // PB-31: el paciente acepta/rechaza su solicitud pendiente. Al aceptar se escribe el meta en RTDB
    // y se otorga autorización al psicólogo (aceptar el chat cuenta como autorizarlo).
    // Solo vale para solicitudes hechas por el psicólogo.
    public async Task<ConversacionDto> ResponderAsync(string? tokenRaw, int idConversacion, bool aceptar, string? ip, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        var c = await chats.FindAsync(idConversacion, ct)
            ?? throw new InvalidOperationException("Conversación no encontrada.");
        if (c.IdPaciente != yo.IdUsuario)
            throw new InvalidOperationException("No tienes permiso sobre esta conversación.");
        if (c.Estado != "Pendiente")
            throw new InvalidOperationException("La solicitud ya fue procesada.");
        if (c.IdSolicitante.HasValue && c.IdSolicitante.Value != c.IdPsicologo)
            throw new InvalidOperationException("Debes esperar la respuesta del psicólogo.");

        await chats.SetEstadoAsync(idConversacion, aceptar ? "Aceptada" : "Rechazada", null, ct);
        if (aceptar)
        {
            await firebase.WriteMetaAsync(idConversacion, c.IdPaciente, c.IdPsicologo, "Aceptada", null, ct);
            var idAut = await historial.GetEstadoIdAsync("Autorizado", ct);
            if (idAut.HasValue)
            {
                await historial.AsegurarAutorizacionAsync(c.IdPaciente, c.IdPsicologo, idAut.Value, ct);
                await authLogs.LogAsync(yo.IdUsuario, c.IdPsicologo, LogAcciones.AutorizacionOtorgada,
                    $"Paciente:{yo.IdUsuario} autorizó al psicólogo:{c.IdPsicologo} al aceptar el chat (conversación {idConversacion})", ip, ct);
            }
        }

        await authLogs.LogAsync(yo.IdUsuario, c.IdPsicologo,
            aceptar ? LogAcciones.ChatAceptado : LogAcciones.ChatRechazado,
            $"Paciente:{yo.IdUsuario} {(aceptar ? "aceptó" : "rechazó")} el chat con psicólogo:{c.IdPsicologo} (conversación {idConversacion})", ip, ct);
        return await ToDtoAsync(idConversacion, yo.IdUsuario, ct);
    }

    // PB-31 doble vía: el paciente solicita chat a un psicólogo desde el directorio.
    // El psicólogo la acepta/rechaza; al aceptar se otorga autorización (simétrico).
    public async Task<ConversacionDto> SolicitarPacienteAsync(string? tokenRaw, int idPsicologo, string? ip, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        await RequireRolAsync(yo.IdUsuario, AppRoles.Paciente, ct);

        var psi = await usuarios.FindByIdAsync(idPsicologo, ct)
            ?? throw new InvalidOperationException("Psicólogo no encontrado.");
        await RequireRolAsync(idPsicologo, AppRoles.Psicologo, ct);
        await RequireAprobadoAsync(idPsicologo, ct);
        await RequireActivaAsync(idPsicologo, ct);

        var existente = await chats.FindParejaAsync(yo.IdUsuario, idPsicologo, ct);
        ConversacionRow row;
        if (existente is null)
        {
            row = await chats.CrearAsync(yo.IdUsuario, idPsicologo, yo.IdUsuario, ct);
        }
        else if (existente.Estado is "Rechazada" or "Cerrada")
        {
            await chats.SetEstadoAsync(existente.IdConversacion, "Pendiente", null, ct);
            row = (await chats.FindAsync(existente.IdConversacion, ct))!;
        }
        else
        {
            throw new InvalidOperationException("Ya existe una solicitud o conversación activa con este psicólogo.");
        }

        await authLogs.LogAsync(yo.IdUsuario, idPsicologo, LogAcciones.ChatSolicitud,
            $"Paciente:{yo.IdUsuario} solicitó chat al psicólogo:{idPsicologo} (conversación {row.IdConversacion})", ip, ct);
        return await ToDtoAsync(row.IdConversacion, yo.IdUsuario, ct);
    }

    // PB-31 doble vía: el psicólogo acepta/rechaza la solicitud que le hizo el paciente.
    // Al aceptar se otorga autorización (simétrico al aceptar del paciente).
    public async Task<ConversacionDto> ResponderPsicologoAsync(string? tokenRaw, int idConversacion, bool aceptar, string? ip, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        var c = await chats.FindAsync(idConversacion, ct)
            ?? throw new InvalidOperationException("Conversación no encontrada.");
        if (c.IdPsicologo != yo.IdUsuario)
            throw new InvalidOperationException("No tienes permiso sobre esta conversación.");
        if (c.Estado != "Pendiente")
            throw new InvalidOperationException("La solicitud ya fue procesada.");
        if (!c.IdSolicitante.HasValue || c.IdSolicitante.Value != c.IdPaciente)
            throw new InvalidOperationException("Esta solicitud no requiere tu respuesta.");

        await chats.SetEstadoAsync(idConversacion, aceptar ? "Aceptada" : "Rechazada", null, ct);
        if (aceptar)
        {
            await firebase.WriteMetaAsync(idConversacion, c.IdPaciente, c.IdPsicologo, "Aceptada", null, ct);
            var idAut = await historial.GetEstadoIdAsync("Autorizado", ct);
            if (idAut.HasValue)
            {
                await historial.AsegurarAutorizacionAsync(c.IdPaciente, c.IdPsicologo, idAut.Value, ct);
                await authLogs.LogAsync(c.IdPaciente, c.IdPsicologo, LogAcciones.AutorizacionOtorgada,
                    $"Paciente:{c.IdPaciente} autorizó al psicólogo:{c.IdPsicologo} al aceptar este su chat (conversación {idConversacion})", ip, ct);
            }
        }

        await authLogs.LogAsync(yo.IdUsuario, c.IdPaciente,
            aceptar ? LogAcciones.ChatAceptado : LogAcciones.ChatRechazado,
            $"Psicólogo:{yo.IdUsuario} {(aceptar ? "aceptó" : "rechazó")} el chat solicitado por paciente:{c.IdPaciente} (conversación {idConversacion})", ip, ct);
        return await ToDtoAsync(idConversacion, yo.IdUsuario, ct);
    }

    // PB-31: el paciente ya no bloquea de un botón: solicita bloqueo con motivo.
    // Queda BloqueoPendiente (mensajes congelados) hasta que el psicólogo o admin lo resuelva.
    public async Task<ConversacionDto> SolicitarBloqueoAsync(string? tokenRaw, int idConversacion, string? motivoRaw, string? ip, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        var c = await chats.FindAsync(idConversacion, ct)
            ?? throw new InvalidOperationException("Conversación no encontrada.");
        if (c.IdPaciente != yo.IdUsuario)
            throw new InvalidOperationException("No tienes permiso sobre esta conversación.");
        if (c.Estado != "Aceptada")
            throw new InvalidOperationException("Solo se puede solicitar bloqueo en un chat aceptado.");
        var motivo = (motivoRaw ?? "").Trim();
        if (string.IsNullOrWhiteSpace(motivo))
            throw new InvalidOperationException("Cuéntanos el motivo de la solicitud.");
        if (motivo.Length > 500)
            throw new InvalidOperationException("El motivo no puede superar los 500 caracteres.");

        await chats.SolicitarBloqueoAsync(idConversacion, motivo, ct);
        await firebase.WriteMetaAsync(idConversacion, c.IdPaciente, c.IdPsicologo, "BloqueoPendiente", yo.IdUsuario, ct);
        await authLogs.LogAsync(yo.IdUsuario, c.IdPsicologo, LogAcciones.AlertaBloqueo,
            $"Paciente:{yo.IdUsuario} solicitó bloqueo al psicólogo:{c.IdPsicologo} (conversación {idConversacion}): {motivo}", ip, ct);
        return await ToDtoAsync(idConversacion, yo.IdUsuario, ct);
    }

    // PB-31: el psicólogo (o admin) resuelve la solicitud: confirma el bloqueo o lo rechaza.
    // El paciente puede cancelar su propia solicitud (aceptar=false).
    public async Task<ConversacionDto> ResolverBloqueoAsync(string? tokenRaw, int idConversacion, bool aceptar, string? ip, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        var c = await chats.FindAsync(idConversacion, ct)
            ?? throw new InvalidOperationException("Conversación no encontrada.");
        if (c.Estado != "BloqueoPendiente")
            throw new InvalidOperationException("No hay solicitud de bloqueo pendiente.");
        var rol = await usuarios.GetRolNombreAsync(yo.IdRol, ct);
        var esAdmin = string.Equals(rol, AppRoles.Administrador, StringComparison.OrdinalIgnoreCase);
        if (yo.IdUsuario == c.IdPaciente)
        {
            if (aceptar)
                throw new InvalidOperationException("No puedes confirmar tu propia solicitud.");
        }
        else if (yo.IdUsuario != c.IdPsicologo && !esAdmin)
        {
            throw new InvalidOperationException("No tienes permiso sobre esta conversación.");
        }

        if (aceptar)
        {
            await chats.SetEstadoAsync(idConversacion, "Bloqueada", c.IdPaciente, ct);
            await firebase.WriteMetaAsync(idConversacion, c.IdPaciente, c.IdPsicologo, "Bloqueada", c.IdPaciente, ct);
        }
        else
        {
            await chats.SetEstadoAsync(idConversacion, "Aceptada", null, ct);
            await firebase.WriteMetaAsync(idConversacion, c.IdPaciente, c.IdPsicologo, "Aceptada", null, ct);
        }
        await authLogs.LogAsync(yo.IdUsuario, c.IdPaciente, LogAcciones.ChatBloqueo,
            $"Usuario:{yo.IdUsuario} {(aceptar ? "confirmó el bloqueo" : "rechazó la solicitud de bloqueo")} (conversación {idConversacion})", ip, ct);
        return await ToDtoAsync(idConversacion, yo.IdUsuario, ct);
    }

    // PB-31: el paciente desbloquea (vuelve a Aceptada).
    public async Task<ConversacionDto> DesbloquearAsync(string? tokenRaw, int idConversacion, string? ip, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        var c = await chats.FindAsync(idConversacion, ct)
            ?? throw new InvalidOperationException("Conversación no encontrada.");
        if (c.IdPaciente != yo.IdUsuario || c.Estado != "Bloqueada" || c.IdBloqueador != yo.IdUsuario)
            throw new InvalidOperationException("No puedes desbloquear esta conversación.");

        await chats.SetEstadoAsync(idConversacion, "Aceptada", null, ct);
        await firebase.WriteMetaAsync(idConversacion, c.IdPaciente, c.IdPsicologo, "Aceptada", null, ct);
        await authLogs.LogAsync(yo.IdUsuario, c.IdPsicologo, LogAcciones.ChatDesbloqueo,
            $"Paciente:{yo.IdUsuario} desbloqueó al psicólogo:{c.IdPsicologo} (conversación {idConversacion})", ip, ct);
        return await ToDtoAsync(idConversacion, yo.IdUsuario, ct);
    }

    // PB-31: aviso best-effort de envío (el mensaje viaja por RTDB; esto deja auditoría en SQL).
    public async Task AvisarMensajeAsync(string? tokenRaw, int idConversacion, string? ip, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        var c = await chats.FindAsync(idConversacion, ct)
            ?? throw new InvalidOperationException("Conversación no encontrada.");
        if (c.IdPaciente != yo.IdUsuario && c.IdPsicologo != yo.IdUsuario)
            throw new InvalidOperationException("No tienes permiso sobre esta conversación.");
        if (c.Estado == "Bloqueada" && c.IdBloqueador != yo.IdUsuario)
            throw new InvalidOperationException("Has sido bloqueado por este usuario.");
        if (c.Estado == "BloqueoPendiente")
            throw new InvalidOperationException("Solicitud de bloqueo en revisión: el chat está pausado.");
        if (c.Estado != "Aceptada" && !(c.Estado == "Bloqueada" && c.IdBloqueador == yo.IdUsuario))
            throw new InvalidOperationException("El chat no está aceptado.");

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario == c.IdPaciente ? c.IdPsicologo : c.IdPaciente,
            LogAcciones.ChatMensaje, $"Mensaje enviado en conversación {idConversacion} por usuario:{yo.IdUsuario}", ip, ct);
    }

    // PB-31: buscador multifiltro (nombre opcional; exige al menos un filtro).
    // Solo psicólogo aprobado y activo.
    public async Task<List<PacienteChatDto>> BuscarPacientesAsync(string? tokenRaw, string? q, string? nivel, int? minCuestionarios, bool? tienePsicologo, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        await RequireRolAsync(yo.IdUsuario, AppRoles.Psicologo, ct);
        await RequireAprobadoAsync(yo.IdUsuario, ct);
        await RequireActivaAsync(yo.IdUsuario, ct);

        var n = NormalizarNivelChat(nivel);
        if (string.IsNullOrWhiteSpace(q) && n is null && minCuestionarios is null && tienePsicologo is null)
            throw new InvalidOperationException("Seleccione al menos un filtro antes de buscar.");

        var idRol = await catalogos.GetRolIdAsync(AppRoles.Paciente, ct);
        var idActiva = await catalogos.GetEstadoIdAsync(AccountStates.Activa, ct);
        var rows = await chats.BuscarPacientesAsync(idRol, idActiva, q, n, minCuestionarios, tienePsicologo, ct);
        return rows.Select(r => new PacienteChatDto
        {
            Id = r.Id,
            Nombre = r.Nombre,
            // Privacidad: el buscador muestra el correo enmascarado.
            Correo = EnmascararCorreo(r.Correo),
            UltimoPuntaje = r.UltimoPuntaje,
            UltimoNivel = r.UltimoNivel,
            UltimaFecha = r.UltimaFecha,
            CuestionariosRespondidos = r.CuestionariosRespondidos,
            Psicologos = r.Psicologos
        }).ToList();
    }

    private static string EnmascararCorreo(string? correo)
    {
        if (string.IsNullOrWhiteSpace(correo)) return string.Empty;
        var at = correo.IndexOf('@');
        if (at <= 0) return "***";
        return $"{correo[0]}***{correo[at..]}";
    }

    private static string? NormalizarNivelChat(string? nivel)
    {
        if (string.IsNullOrWhiteSpace(nivel)) return null;
        var n = nivel.Trim();
        if (string.Equals(n, "bajo", StringComparison.OrdinalIgnoreCase)) return "Bajo";
        if (string.Equals(n, "medio", StringComparison.OrdinalIgnoreCase)) return "Medio";
        if (string.Equals(n, "alto", StringComparison.OrdinalIgnoreCase)) return "Alto";
        return null;
    }

    private async Task<ConversacionDto> ToDtoAsync(int idConversacion, int yoId, CancellationToken ct)
    {
        var c = (await chats.FindAsync(idConversacion, ct))!;
        var otroId = c.IdPaciente == yoId ? c.IdPsicologo : c.IdPaciente;
        var otro = await usuarios.FindByIdAsync(otroId, ct);
        return new ConversacionDto
        {
            Id = c.IdConversacion,
            Estado = c.Estado,
            BloqueadaPorMi = c.IdBloqueador == yoId,
            IdSolicitante = c.IdSolicitante,
            MotivoBloqueo = c.MotivoBloqueo,
            Contraparte = new ChatContraparteDto
            {
                Id = otroId,
                Nombre = otro is null ? $"Usuario:{otroId}" : $"{otro.Nombre} {otro.Apellido}".Trim(),
                Rol = otro is null ? "" : await catalogos.GetRolNombreAsync(otro.IdRol, ct) ?? ""
            },
            FechaCreacion = c.FechaCreacion
        };
    }

    private async Task<UsuarioDto> RequireSesionAsync(string? tokenRaw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenRaw))
            throw new UnauthorizedAccessException("Sesión requerida");
        var sesion = await sesiones.FindByTokenHashAsync(TokenHelper.Hash(tokenRaw.Trim()), ct);
        if (sesion is null || sesion.ExpiraEn < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Sesión requerida");
        return await usuarios.FindByIdAsync(sesion.IdUsuario, ct)
            ?? throw new UnauthorizedAccessException("Sesión requerida");
    }

    private async Task RequireRolAsync(int idUsuario, string rol, CancellationToken ct)
    {
        var u = await usuarios.FindByIdAsync(idUsuario, ct);
        var nombre = u is null ? null : await catalogos.GetRolNombreAsync(u.IdRol, ct);
        if (!string.Equals(nombre, rol, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Se requiere rol {rol}.");
    }

    private async Task RequireActivaAsync(int idUsuario, CancellationToken ct)
    {
        var u = await usuarios.FindByIdAsync(idUsuario, ct);
        var nombre = u is null ? null : await usuarios.GetEstadoCuentaNombreAsync(u.IdEstadoCuenta, ct);
        if (!string.Equals(nombre, AccountStates.Activa, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La cuenta no está activa.");
    }

    private async Task RequireAprobadoAsync(int idUsuario, CancellationToken ct)
    {
        var u = await usuarios.FindByIdAsync(idUsuario, ct);
        if (u?.IdEstadoAprobacion is null) return; // pacientes no tienen aprobación
        var nombre = await catalogos.GetEstadoNombreAsync(u.IdEstadoAprobacion.Value, ct);
        if (!string.Equals(nombre, AccountStates.Aprobado, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("El psicólogo aún no está aprobado.");
    }
}
