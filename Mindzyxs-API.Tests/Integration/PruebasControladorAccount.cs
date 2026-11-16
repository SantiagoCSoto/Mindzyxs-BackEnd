using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasControladorAccount : IClassFixture<FabricaAplicacionWebApiConDatos>
{
    private readonly HttpClient _client;

    public PruebasControladorAccount(FabricaAplicacionWebApiConDatos factory)
    {
        factory.SeedDatabase();
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Catalogos_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/account/catalogos");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_CamposFaltantes_ReturnsBadRequest()
    {
        var content = new MultipartFormDataContent();
        var response = await _client.PostAsync("/api/account/register", content);
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Verify_CodigoInvalido_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/account/verify", new { correo = "paciente@test.com", codigo = "000000" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Me_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/account/me");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_SinToken_ReturnsOk()
    {
        var response = await _client.PostAsync("/api/account/logout", null);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Forgot_CorreoInvalido_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/account/forgot", new { correo = "invalid" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Forgot_CorreoInexistente_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/account/forgot", new { correo = "noexiste@test.com" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReanudarVerificacion_CorreoInvalido_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/account/verificacion/reanudar", new { correo = "" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
