using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

[Collection("SqlIntegration")]
public class PruebasIntegracionSqlAccount : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly HttpClient _client;

    public PruebasIntegracionSqlAccount(FabricaAplicacionWebApiSql factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_CorreoDuplicado_ReturnsConflict()
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("Paciente"), "TipoUsuario");
        content.Add(new StringContent("Dup"), "Nombre");
        content.Add(new StringContent("User"), "Apellido");
        content.Add(new StringContent("paciente@test.com"), "Correo");
        content.Add(new StringContent("Abc12345"), "Password");
        content.Add(new StringContent("2000-01-01"), "FechaNacimiento");
        content.Add(new StringContent("1"), "IdGenero");

        var response = await _client.PostAsync("/api/account/register", content);
        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Login_CamposFaltantes_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/account/login", new { correo = "", password = "" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}



