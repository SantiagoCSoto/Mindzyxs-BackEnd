using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Recordatorios del paciente (PB-15 HU1-PB15): configuración de frecuencia y
/// horario para el registro emocional, con activación/desactivación y endpoint
/// de avisos vencidos-hoy para polling del front.
/// </summary>
[ApiController]
[Route("api/recordatorios")] // plural explícito: [controller] daría singular
public class RecordatorioController(RecordatorioService recordatorios) : ControllerBase
{
    /// <summary>Devuelve mi configuración actual (204 si nunca configuré).</summary>
    /// <remarks>
    /// PB-15 HU1-PB15. Requiere sesión de paciente.
    ///
    /// Respuestas:
    /// - `200` — `{ "frecuencia": "Diaria|Semanal", "hora": "08:00", "dias": [1,3,5], "titulo": ..., "mensaje": ..., "activo": true }`
    /// - `204` — sin configuración.
    /// - `401` — sin sesión.
    /// </remarks>
    /// <response code="200">Configuración actual.</response>
    /// <response code="204">Sin configurar.</response>
    /// <response code="401">Sesión requerida.</response>
    [HttpGet]
    public async Task<IActionResult> Mio(CancellationToken ct)
    {
        try
        {
            var dto = await recordatorios.ObtenerAsync(ReadOptionalBearerToken(), ct);
            return dto is null ? NoContent() : Ok(dto);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Guarda o actualiza mi configuración (upsert).</summary>
    /// <remarks>
    /// PB-15 HU1-PB15. Valida frecuencia y hora; en Semanal exige días 1-7.
    /// Desactivar guarda `activo:false` (deja de avisar). Guarda `RECORDATORIO_CONFIG`.
    ///
    /// Respuestas:
    /// - `200` — `{ "mensaje": "Recordatorios configurados correctamente", ...config }`
    /// - `400` — `{ "error": "Debes seleccionar frecuencia y hora" }`
    /// - `401` — sin sesión.
    /// </remarks>
    /// <response code="200">Configuración guardada.</response>
    /// <response code="400">Faltan datos.</response>
    /// <response code="401">Sesión requerida.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ErrorResult), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Guardar([FromBody] GuardarRecordatorioVm vm, CancellationToken ct)
    {
        if (!ModelState.IsValid || vm is null)
            return BadRequest(new { error = "Debes seleccionar frecuencia y hora" });
        try
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var dto = await recordatorios.GuardarAsync(ReadOptionalBearerToken(), vm, ip, ct);
            return Ok(new
            {
                mensaje = "Recordatorios configurados correctamente",
                frecuencia = dto.Frecuencia,
                hora = dto.Hora,
                dias = dto.Dias,
                activo = dto.Activo
            });
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    /// <summary>Devuelve mis avisos vencidos hoy (para polling del front).</summary>
    /// <remarks>
    /// PB-15 HU1-PB15. Activos, día aplica (siempre en Diaria) y hora ya pasada.
    /// El front lo consulta cada minuto y dispara el aviso en el momento exacto.
    ///
    /// Respuestas:
    /// - `200` — `[{ "titulo": ..., "mensaje": ..., "hora": "08:00" }]` (vacío si nada vence).
    /// - `401` — sin sesión.
    /// </remarks>
    /// <response code="200">Avisos vencidos hoy.</response>
    /// <response code="401">Sesión requerida.</response>
    [HttpGet("hoy")]
    [ProducesResponseType(typeof(List<RecordatorioHoyDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Hoy(CancellationToken ct)
    {
        try
        {
            return Ok(await recordatorios.HoyAsync(ReadOptionalBearerToken(), ct));
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(new { error = ex.Message });
        }
    }

    /// <summary>Lee el token de sesión del header.</summary>
    private string? ReadOptionalBearerToken()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var v)) return null;
        var parts = v.ToString().Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var token = parts.Length == 1 ? parts[0] : parts[^1];
        return token.Trim().Trim('"').Trim('\'');
    }
}
