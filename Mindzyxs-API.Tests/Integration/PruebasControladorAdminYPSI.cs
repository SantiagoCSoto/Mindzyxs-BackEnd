using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorAdminLineasAyuda : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorAdminLineasAyuda(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Listar_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/admin/lineas-ayuda"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Detalle_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/admin/lineas-ayuda/1"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Crear_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/admin/lineas-ayuda", new { }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
}

public class PruebasControladorAdminNotificaciones : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorAdminNotificaciones(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Listar_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/notificaciones"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Detalle_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/notificaciones/CODE_SENT"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
}

public class PruebasControladorAdminUsuarios : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorAdminUsuarios(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Listar_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/usuarios"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
    [Fact] public async Task Detalle_SinToken_ReturnsForbidden() { var r = await _client.GetAsync("/api/admin/usuarios/1"); r.StatusCode.Should().Be(HttpStatusCode.Forbidden); }
}

public class PruebasControladorPsicologoCuestionarios : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;
    public PruebasControladorPsicologoCuestionarios(FabricaAplicacionWebApiConDatos factory) { factory.SeedDatabase(); _client = factory.CreateClient(); }

    [Fact] public async Task Listar_SinToken_ReturnsUnauthorized() { var r = await _client.GetAsync("/api/psicologo/cuestionarios"); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
    [Fact] public async Task Crear_SinToken_ReturnsUnauthorized() { var r = await _client.PostAsJsonAsync("/api/psicologo/cuestionarios", new { }); r.StatusCode.Should().Be(HttpStatusCode.Unauthorized); }
}
