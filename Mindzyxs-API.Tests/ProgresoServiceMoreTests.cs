using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class ProgresoServiceMoreTests
{
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private ProgresoService CreateSut() => new(
        _historial.Object, _users.Object, _sesiones.Object, _authLogs.Object);

    private void SetupSesion(int userId = 5)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 2, IdEstadoCuenta = 1 };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
    }

    [Fact]
    public async Task Autorizados_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.AutorizadosAsync(null, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Autorizados_SinPacientes_RetornaListaVacia()
    {
        SetupSesion();
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.GetPacientesAutorizadosAsync(5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(new List<UsuarioDto>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.AutorizadosAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Autorizados_ConPacientes_RetornaLista()
    {
        SetupSesion();
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.GetPacientesAutorizadosAsync(5, 8, It.IsAny<CancellationToken>())).ReturnsAsync(
            new List<UsuarioDto> { new() { IdUsuario = 1, Nombre = "Camila", Apellido = "Rojas" } });
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.AutorizadosAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Nombre.Should().Be("Camila Rojas");
    }
}
