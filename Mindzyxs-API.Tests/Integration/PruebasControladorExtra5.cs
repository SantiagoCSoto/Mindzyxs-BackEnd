using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorExtra5 : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorExtra5(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task HistorialGet_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/historial"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task HistorialGetById_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/historial/1"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task HistorialPost_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/historial", new { }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task ObservacionesPost_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/observaciones", new { }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task ProgresoPacientes_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/progreso/pacientes"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task ProgresoGet_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/progreso/1"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task ProgresoReporte_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/progreso/1/reporte"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task RecordatorioGet_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/recordatorios"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task RecordatorioPost_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/recordatorios", new { }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task RecordatorioHoy_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/recordatorios/hoy"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task PsicologoRegistrosPacientes_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/psicologo/pacientes"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
}
