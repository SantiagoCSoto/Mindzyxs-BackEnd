using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorExtra3 : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorExtra3(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task InvitadoPanel_ReturnsOk() { var r = await _client.GetAsync("/api/invitado/panel"); r.StatusCode.Should().Be(HttpStatusCode.OK); }
    [Fact] public async Task InvitadoAcceso_SinToken_ReturnsOk() { var r = await _client.PostAsJsonAsync("/api/invitado/acceso", new { origen = "test", resultado = "ok" }); r.StatusCode.Should().Be(HttpStatusCode.OK); }
    [Fact] public async Task InvitadoBloqueo_SinToken_ReturnsOk() { var r = await _client.PostAsJsonAsync("/api/invitado/bloqueo", new { funcion = "historial" }); r.StatusCode.Should().Be(HttpStatusCode.OK); }
    [Fact] public async Task InvitadoSalida_SinToken_ReturnsOk() { var r = await _client.PostAsJsonAsync("/api/invitado/salida", new { origen = "panel" }); r.StatusCode.Should().Be(HttpStatusCode.OK); }
    [Fact] public async Task AvisoGet_ReturnsOk() { var r = await _client.GetAsync("/api/aviso"); r.StatusCode.Should().Be(HttpStatusCode.OK); }
    [Fact] public async Task AyudaLineas_ReturnsOk() { var r = await _client.GetAsync("/api/ayuda/lineas"); r.StatusCode.Should().Be(HttpStatusCode.OK); }
    [Fact] public async Task Health_ReturnsOk() { var r = await _client.GetAsync("/api/health"); r.StatusCode.Should().Be(HttpStatusCode.OK); }
}
