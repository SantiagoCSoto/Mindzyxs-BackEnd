using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class RecordatorioService(
    IRecordatorioRepository recordatorios,
    IUserRepository usuarios,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs)
{
    // PB-15 HU1-PB15: mi configuración actual (con días si es Semanal).
    public async Task<RecordatorioDto?> ObtenerAsync(string? tokenRaw, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        var row = await recordatorios.GetDePacienteAsync(yo.IdUsuario, ct);
        if (row is null) return null;
        return new RecordatorioDto
        {
            Frecuencia = row.Frecuencia,
            Hora = row.Hora.ToString(@"hh\:mm"),
            Dias = row.Frecuencia == "Semanal" ? await recordatorios.GetDiasAsync(row.IdRecordatorio, ct) : [],
            Titulo = row.Titulo,
            Mensaje = row.Mensaje,
            Activo = row.Activo
        };
    }

    // PB-15 HU1-PB15: guarda o actualiza mi configuración (upsert).
    // Sin frecuencia u hora -> "Debes seleccionar frecuencia y hora".
    // Lanza InvalidOperationException con el mensaje exacto para el 400.
    public async Task<RecordatorioDto> GuardarAsync(string? tokenRaw, GuardarRecordatorioVm vm, string? ip, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        var frecuencia = vm.Frecuencia?.Trim();
        if (string.IsNullOrWhiteSpace(frecuencia) || string.IsNullOrWhiteSpace(vm.Hora)
            || !TimeSpan.TryParse(vm.Hora, out var hora))
            throw new InvalidOperationException("Debes seleccionar frecuencia y hora");
        if (frecuencia != "Diaria" && frecuencia != "Semanal")
            throw new InvalidOperationException("Debes seleccionar frecuencia y hora");

        var dias = frecuencia == "Semanal" ? vm.Dias.Where(d => d is >= 1 and <= 7).Distinct().ToList() : [];
        if (frecuencia == "Semanal" && dias.Count == 0)
            throw new InvalidOperationException("Debes seleccionar frecuencia y hora");

        var row = await recordatorios.GuardarAsync(
            yo.IdUsuario,
            string.IsNullOrWhiteSpace(vm.Titulo) ? "Registro emocional" : vm.Titulo.Trim(),
            vm.Mensaje,
            hora, frecuencia, vm.Activo ?? true, dias, ct);

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.RecordatorioConfig,
            $"Paciente:{yo.IdUsuario} guardó recordatorios ({frecuencia} {row.Hora:hh\\:mm}, activo: {row.Activo})", ip, ct);

        return new RecordatorioDto
        {
            Frecuencia = row.Frecuencia,
            Hora = row.Hora.ToString(@"hh\:mm"),
            Dias = dias,
            Titulo = row.Titulo,
            Mensaje = row.Mensaje,
            Activo = row.Activo
        };
    }

    // PB-15 HU1-PB15: mis avisos vencidos hoy (activos, día aplica, hora ya pasó).
    // El front lo consulta por polling y dispara el aviso en el momento programado.
    public async Task<List<RecordatorioHoyDto>> HoyAsync(string? tokenRaw, CancellationToken ct = default)
    {
        var yo = await RequireSesionAsync(tokenRaw, ct);
        var row = await recordatorios.GetDePacienteAsync(yo.IdUsuario, ct);
        if (row is null || !row.Activo) return [];

        var ahora = DateTime.Now;
        var dia = ((int)ahora.DayOfWeek - 1 + 7) % 7 + 1; // 1=Lunes..7=Domingo
        if (row.Frecuencia == "Semanal")
        {
            var dias = await recordatorios.GetDiasAsync(row.IdRecordatorio, ct);
            if (!dias.Contains(dia)) return [];
        }
        if (ahora.TimeOfDay < row.Hora) return [];

        return
        [
            new RecordatorioHoyDto
            {
                Titulo = row.Titulo,
                Mensaje = row.Mensaje,
                Hora = row.Hora.ToString(@"hh\:mm")
            }
        ];
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
}
