using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorExtra4 : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorExtra4(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task AdminLineasAyuda_Detalle_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/admin/lineas-ayuda/1"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task AdminLineasAyuda_Activar_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsync("/api/admin/lineas-ayuda/1/activar", null); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task AdminLineasAyuda_Inactivar_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsync("/api/admin/lineas-ayuda/1/inactivar", null); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task AdminLineasAyuda_Eliminar_SinToken_ReturnsUnauthorized() { var r = await _client.DeleteAsync("/api/admin/lineas-ayuda/1"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task AdminLineasAyuda_Actualizar_SinToken_ReturnsUnauthorized() { var r = await _client.PutAsJsonAsync("/api/admin/lineas-ayuda/1", new { }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task AdminCuestionarios_Actualizar_SinToken_ReturnsUnauthorized() { var r = await _client.PutAsJsonAsync("/api/admin/cuestionarios-predeterminados/1", new { }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task AdminCuestionarios_Desactivar_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsync("/api/admin/cuestionarios-predeterminados/1/desactivar", null); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Admin_BackupDownload_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/backups/1/download"); r.StatusCode.Should().BeOneOf(HttpStatusCode.Forbidden, HttpStatusCode.NotFound, HttpStatusCode.Unauthorized); }
    [Fact] public async Task Admin_TarjetaVerificar_SinToken_ReturnsForbidden() { var r = await _client.PutAsync("/api/admin/psicologos/1/tarjeta-verificada", null); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Admin_Rechazar_SinToken_ReturnsForbidden() { var r = await _client.PostAsJsonAsync("/api/admin/psicologos/1/rechazar", new { }); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Admin_CambiarEstado_SinToken_ReturnsForbidden() { var r = await _client.PutAsJsonAsync("/api/admin/usuarios/1/estado", new { nuevoEstado = "Activa" }); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Admin_ForzarReset_SinToken_ReturnsForbidden() { var r = await _client.PostAsync("/api/admin/usuarios/1/reset-password", null); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Admin_ActualizarNotificacion_SinToken_ReturnsForbidden() { var r = await _client.PutAsJsonAsync("/api/admin/notificaciones/CODE_SENT", new { activo = true, canales = new[] { "correo" }, asunto = "a", cuerpo = "b" }); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Admin_PreviewNotificacion_SinToken_ReturnsForbidden() { var r = await _client.PostAsJsonAsync("/api/admin/notificaciones/CODE_SENT/preview", new { }); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Admin_DetalleValidacion_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/psicologos/1/validacion"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Admin_TarjetaPath_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/psicologos/1/tarjeta"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
}
