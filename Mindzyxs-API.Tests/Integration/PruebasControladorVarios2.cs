using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorHistorial : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorHistorial(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Get_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/historial"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task GetById_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/historial/1"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
}

public class PruebasControladorObservaciones : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorObservaciones(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Post_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/observaciones", new { }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
}

public class PruebasControladorProgreso : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorProgreso(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Pacientes_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/progreso/pacientes"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Get_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/progreso/1"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
}

public class PruebasControladorRecordatorio : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorRecordatorio(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Get_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/recordatorios"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Hoy_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/recordatorios/hoy"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
}
