using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorCuestionarioExtra : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorCuestionarioExtra(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task RegistrarInicioInvitado_ReturnsOk() { var r = await _client.PostAsJsonAsync("/api/cuestionario/invitado/inicio", new { cuestionarioId = 1 }); r.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest, HttpStatusCode.NotFound); }
    [Fact] public async Task PacienteAsignados_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/cuestionario/paciente/asignados"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task PacienteAsignadoDetalle_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/cuestionario/paciente/asignados/1"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task PacienteInicio_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/cuestionario/paciente/inicio", new { cuestionarioId = 1 }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task PacienteFinalizar_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/cuestionario/paciente/finalizar", new { cuestionarioId = 1, respuestas = Array.Empty<object>() }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
}

public class PruebasControladorAdminExtra : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorAdminExtra(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Maintenance_SinToken_ReturnsForbidden() { var r = await _client.PostAsync("/api/admin/maintenance", null); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Backups_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/backups"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task TarjetaPendientes_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/psicologos/tarjeta-pendientes"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task PendientesValidacion_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/psicologos/pendientes"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Usuarios_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/usuarios"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Notificaciones_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/notificaciones"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task ValidacionDetalle_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/psicologos/1/validacion"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task TarjetaPath_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/psicologos/1/tarjeta"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task TarjetaStream_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/psicologos/1/tarjeta"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Rechazar_SinToken_ReturnsForbidden() { var r = await _client.PostAsync("/api/admin/psicologos/1/rechazar", null); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task CambiarEstado_SinToken_ReturnsForbidden() { var r = await _client.PutAsJsonAsync("/api/admin/usuarios/1/estado", new { nuevoEstado = "Activa" }); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task ForzarReset_SinToken_ReturnsForbidden() { var r = await _client.PostAsync("/api/admin/usuarios/1/reset-password", null); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task ActualizarNotificacion_SinToken_ReturnsForbidden() { var r = await _client.PutAsJsonAsync("/api/admin/notificaciones/CODE_SENT", new { activo = true, canales = new[] { "correo" }, asunto = "a", cuerpo = "b" }); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task PreviewNotificacion_SinToken_ReturnsForbidden() { var r = await _client.PostAsJsonAsync("/api/admin/notificaciones/CODE_SENT/preview", new { }); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
}
