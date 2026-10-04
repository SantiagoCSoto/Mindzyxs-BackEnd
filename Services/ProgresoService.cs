using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class ProgresoService(
    IHistorialRepository historial,
    IUserRepository usuarios,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs)
{
    // PB-14 HU1-PB14: pacientes que autorizaron al psicólogo.
    // Vacía -> el front muestra "No tienes pacientes autorizados en este momento".
    public async Task<List<ProgresoPacienteDto>> AutorizadosAsync(string? tokenRaw, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);
        var idAut = await historial.GetEstadoIdAsync("Autorizado", ct) ?? -1;
        var lista = await historial.GetPacientesAutorizadosAsync(yo.IdUsuario, idAut, ct);

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.ProgresoConsulta,
            $"Psicólogo:{yo.IdUsuario} consultó sus pacientes autorizados ({lista.Count})", ip, ct);

        return lista.Select(u => new ProgresoPacienteDto
        {
            Id = u.IdUsuario,
            Nombre = $"{u.Nombre} {u.Apellido}".Trim()
        }).ToList();
    }

    // PB-14 HU1-PB14: agregados del paciente autorizado en el rango.
    // Sin autorización -> 403 "No tienes autorización para ver los registros de este paciente".
    public async Task<ProgresoDto> ObtenerAsync(string? tokenRaw, int idPaciente, DateTime? desde, DateTime? hasta, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);
        var idAut = await historial.GetEstadoIdAsync("Autorizado", ct) ?? -1;
        if (!await historial.EstaAutorizadoAsync(idPaciente, yo.IdUsuario, idAut, ct))
            throw new InvalidOperationException("No tienes autorización para ver los registros de este paciente.");

        var eventos = await historial.GetEventosAsync(idPaciente, desde, hasta, ct);
        var niveles = await historial.GetNivelesAsync(ct);

        var dto = new ProgresoDto { TotalRegistros = eventos.Count };
        if (eventos.Count == 0) return dto;

        var puntos = eventos
            .OrderBy(e => e.FechaEvento)
            .Select(e => new ProgresoPuntoDto
            {
                Fecha = e.FechaEvento,
                Puntaje = e.PuntajeTotal,
                Nivel = e.IdNivelRiesgo.HasValue && niveles.TryGetValue(e.IdNivelRiesgo.Value, out var n) ? n : null
            }).ToList();
        dto.Serie = puntos;

        var puntajes = puntos.Where(p => p.Puntaje.HasValue).Select(p => (double)p.Puntaje!.Value).ToList();
        if (puntajes.Count > 0)
        {
            dto.Promedio = Math.Round(puntajes.Average(), 2);
            if (puntajes.Count >= 2)
            {
                var mitad = puntajes.Count / 2;
                var antes = puntajes.Take(mitad).Average();
                var despues = puntajes.Skip(mitad).Average();
                dto.Tendencia = despues - antes > 0.5 ? "Sube" : antes - despues > 0.5 ? "Baja" : "Estable";
            }
            dto.PorSemana = puntajes
                .Zip(puntos.Where(p => p.Puntaje.HasValue), (v, p) => (v, p.Fecha))
                .GroupBy(x => InicioSemana(x.Fecha))
                .OrderBy(g => g.Key)
                .Select(g => new ProgresoSemanaDto
                {
                    Semana = g.Key.ToString("yyyy-MM-dd"),
                    Registros = g.Count(),
                    Promedio = Math.Round(g.Average(x => x.v), 2)
                }).ToList();
        }

        foreach (var p in puntos.Where(p => p.Nivel != null))
        {
            var nivel = p.Nivel!;
            dto.Distribucion[nivel] = dto.Distribucion.TryGetValue(nivel, out var c) ? c + 1 : 1;
        }

        // PB-23: nivel más frecuente
        if (dto.Distribucion.Count > 0)
            dto.NivelFrecuente = dto.Distribucion.OrderByDescending(kv => kv.Value).First().Key;

        // PB-23: agregados por mes
        if (puntajes.Count > 0)
        {
            dto.PorMes = puntos
                .Where(p => p.Puntaje.HasValue)
                .GroupBy(p => new { p.Fecha.Year, p.Fecha.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new ProgresoMesDto
                {
                    Mes = $"{g.Key.Year:D4}-{g.Key.Month:D2}",
                    Registros = g.Count(),
                    Promedio = Math.Round(g.Average(p => (double)p.Puntaje!.Value), 2)
                })
                .ToList();
        }

        dto.Suficientes = eventos.Count >= 3;

        await authLogs.LogAsync(yo.IdUsuario, idPaciente, LogAcciones.EstadisticasConsulta,
            $"Psicólogo:{yo.IdUsuario} consultó estadísticas del paciente:{idPaciente} ({eventos.Count} registros)", ip, ct);

        return dto;
    }

    private static DateTime InicioSemana(DateTime fecha)
    {
        var d = fecha.Date;
        var diff = ((int)d.DayOfWeek - 1 + 7) % 7; // lunes = inicio
        return d.AddDays(-diff);
    }

    private async Task<UsuarioDto> RequirePsicologoAsync(string? tokenRaw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenRaw))
            throw new UnauthorizedAccessException("Sesión requerida");
        var sesion = await sesiones.FindByTokenHashAsync(TokenHelper.Hash(tokenRaw.Trim()), ct);
        if (sesion is null || sesion.ExpiraEn < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Sesión requerida");
        var user = await usuarios.FindByIdAsync(sesion.IdUsuario, ct)
            ?? throw new UnauthorizedAccessException("Sesión requerida");
        return user;
    }
}
