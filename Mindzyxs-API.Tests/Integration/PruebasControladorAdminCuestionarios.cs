using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorAdminCuestionarios : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorAdminCuestionarios(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Listar_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/admin/cuestionarios-predeterminados"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Detalle_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/admin/cuestionarios-predeterminados/1"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Crear_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/admin/cuestionarios-predeterminados", new { }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Desactivar_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsync("/api/admin/cuestionarios-predeterminados/1/desactivar", null); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
}
