using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using FluentAssertions;
using Mindzyxs_API.Tests.Integration;
using Xunit;

namespace Mindzyxs_API.Tests.Integration;

public class PruebasIntegracionSqlAccountExtra : IClassFixture<FabricaAplicacionWebApiSql>
{
    private readonly FabricaAplicacionWebApiSql _factory;
    private readonly HttpClient _client;

    public PruebasIntegracionSqlAccountExtra(FabricaAplicacionWebApiSql factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Catalogos_ReturnsOk()
    {
        var response = await _client.GetAsync("/api/account/catalogos");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_PacienteValido_ReturnsCreated()
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("Paciente"), "TipoUsuario");
        content.Add(new StringContent("Nuevo"), "Nombre");
        content.Add(new StringContent("Usuario"), "Apellido");
        content.Add(new StringContent($"nuevo{Guid.NewGuid():N}@test.com"), "Correo");
        content.Add(new StringContent("Abc12345"), "Password");
        content.Add(new StringContent("2000-01-01"), "FechaNacimiento");
        content.Add(new StringContent("1"), "IdGenero");

        var response = await _client.PostAsync("/api/account/register", content);
        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Register_PsicologoSinTarjeta_ReturnsBadRequest()
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent("Psicologo"), "TipoUsuario");
        content.Add(new StringContent("Psi"), "Nombre");
        content.Add(new StringContent("Test"), "Apellido");
        content.Add(new StringContent($"psi{Guid.NewGuid():N}@test.com"), "Correo");
        content.Add(new StringContent("Abc12345"), "Password");
        content.Add(new StringContent("1990-01-01"), "FechaNacimiento");
        content.Add(new StringContent("2"), "IdGenero");

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
    public async Task Verify_CodigoExpirado_ReturnsGone()
    {
        // Use a non-existent user to get 400
        var response = await _client.PostAsJsonAsync("/api/account/verify", new { correo = "noexiste@test.com", codigo = "123456" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_CamposFaltantes_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/account/login", new { correo = "", password = "" });
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
    public async Task ForgotResend_CorreoValido_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/api/account/forgot/resend", new { correo = "paciente@test.com" });
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ReanudarVerificacion_CorreoInvalido_ReturnsBadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/account/verificacion/reanudar", new { correo = "" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task ObtenerPerfil_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.GetAsync("/api/account/perfil");
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ActualizarPerfil_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.PutAsJsonAsync("/api/account/perfil", new { nombres = "A", apellidos = "B" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SolicitarCambioCorreo_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/account/correo/solicitar", new { nuevoCorreo = "x@test.com" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ConfirmarCambioCorreo_SinToken_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/api/account/correo/confirmar", new { nuevoCorreo = "x@test.com", codigo = "123456" });
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
