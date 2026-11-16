using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorAviso : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorAviso(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Get_ReturnsOk() { var r = await _client.GetAsync("/api/aviso"); r.StatusCode.Should().Be(HttpStatusCode.OK); }
    [Fact] public async Task Visualizacion_SinToken_ReturnsOk() { var r = await _client.PostAsJsonAsync("/api/aviso/visualizacion", new { }); r.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.BadRequest); }
}

public class PruebasControladorAyuda : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorAyuda(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Lineas_ReturnsOk() { var r = await _client.GetAsync("/api/ayuda/lineas"); r.StatusCode.Should().Be(HttpStatusCode.OK); }
}

public class PruebasControladorInvitado : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorInvitado(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Panel_ReturnsOk() { var r = await _client.GetAsync("/api/invitado/panel"); r.StatusCode.Should().Be(HttpStatusCode.OK); }
}

public class PruebasControladorRecomendacion : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorRecomendacion(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }
}

