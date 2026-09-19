using Microsoft.AspNetCore.Mvc;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Services;

namespace Mindzyxs_API.Controllers;

/// <summary>
/// Ayuda al usuario (PB-08 HU1-PB8): líneas de ayuda activas para invitado y pacientes.
/// El contenido lo administra el administrador (PB-29). Endpoint público.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AyudaController(RiesgoService riesgo) : ControllerBase
{
    /// <summary>Devuelve las líneas de ayuda en estado Activa.</summary>
    /// <remarks>
    /// PB-08 HU1-PB8. Público. Usado por el botón/modal "Líneas de ayuda" del Panel de
    /// invitado y de la pantalla Resultado del riesgo (resaltado urgente si el riesgo es Alto).
    ///
    /// Respuestas:
    /// - `200` — `[ { "id": ..., "nombre": ..., "telefono": ..., "url": ..., "horario": ..., "cobertura": ..., "descripcion": ... } ]`
    /// </remarks>
    /// <response code="200">Líneas de ayuda activas.</response>
    [HttpGet("lineas")]
    [ProducesResponseType(typeof(List<LineaAyudaDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<LineaAyudaDto>>> Lineas(CancellationToken ct)
        => Ok(await riesgo.ObtenerLineasAsync(ct));
}
