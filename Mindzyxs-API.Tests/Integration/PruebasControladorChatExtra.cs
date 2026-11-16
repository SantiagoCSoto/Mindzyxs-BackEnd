using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorChatExtra : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorChatExtra(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Conversaciones_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/chat/conversaciones"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Solicitudes_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/chat/solicitudes", new { idPaciente = 1 }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task SolicitudesPaciente_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/chat/solicitudes/paciente", new { idPsicologo = 1 }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Pacientes_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/chat/pacientes"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Aceptar_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsync("/api/chat/solicitudes/1/aceptar", null); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Rechazar_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsync("/api/chat/solicitudes/1/rechazar", null); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task AceptarPsicologo_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsync("/api/chat/solicitudes/1/aceptar-psicologo", null); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task RechazarPsicologo_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsync("/api/chat/solicitudes/1/rechazar-psicologo", null); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task BloqueoSolicitar_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/chat/conversaciones/1/bloqueo/solicitar", new { motivo = "x" }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task BloqueoResolver_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/chat/conversaciones/1/bloqueo/resolver", new { }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task BloqueoCancelar_SinToken_ReturnsUnauthorized() { var r = await _client.DeleteAsync("/api/chat/conversaciones/1/bloqueo"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task AvisarMensaje_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsync("/api/chat/conversaciones/1/mensajes/aviso", null); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
}
