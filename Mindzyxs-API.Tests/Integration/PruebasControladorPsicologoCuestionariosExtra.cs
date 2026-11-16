using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorPsicologoCuestionariosExtra : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorPsicologoCuestionariosExtra(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Detalle_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/psicologo/cuestionarios/1"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Despublicar_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsync("/api/psicologo/cuestionarios/1/despublicar", null); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Actualizar_SinToken_ReturnsUnauthorized() { var r = await _client.PutAsJsonAsync("/api/psicologo/cuestionarios/1", new { }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Eliminar_SinToken_ReturnsUnauthorized() { var r = await _client.DeleteAsync("/api/psicologo/cuestionarios/1"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task AsignarPantalla_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/psicologo/cuestionarios/1/asignar"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Asignar_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/psicologo/cuestionarios/1/asignar", new { pacientesIds = new[] { 1 } }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
}
