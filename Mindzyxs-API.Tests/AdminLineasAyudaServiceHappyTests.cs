using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AdminLineasAyudaServiceHappyTests
{
    private readonly Mock<ILineaAyudaRepository> _lineas = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private AdminLineasAyudaService CreateSut() => new(
        _lineas.Object, _users.Object, _sesiones.Object, _authLogs.Object);

    private void SetupAdminSesion(int userId = 1)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 3, IdEstadoCuenta = 1 };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Administrador");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
    }

    [Fact]
    public async Task Listar_HappyPath_RetornaLista()
    {
        SetupAdminSesion();
        _lineas.Setup(l => l.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<LineaAyudaRow>
        {
            new() { IdLinea = 1, Nombre = "L1", Estado = "Activa" }
        });

        var sut = CreateSut();
        var result = await sut.ListarAsync("token", null, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Nombre.Should().Be("L1");
    }

    [Fact]
    public async Task ObtenerDetalle_HappyPath_RetornaDetalle()
    {
        SetupAdminSesion();
        _lineas.Setup(l => l.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            new LineaAyudaRow { IdLinea = 1, Nombre = "L1", Estado = "Activa" });

        var sut = CreateSut();
        var result = await sut.ObtenerDetalleAsync("token", 1, CancellationToken.None);

        result.Id.Should().Be(1);
        result.Nombre.Should().Be("L1");
    }

    [Fact]
    public async Task Crear_HappyPath_RetornaId()
    {
        SetupAdminSesion();
        _lineas.Setup(l => l.CrearAsync(It.IsAny<LineaAyudaRow>(), It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var dto = new LineaAyudaAdminCrearDto { Nombre = "Nueva", Telefono = "123" };
        var result = await sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        result.Id.Should().Be(5);
    }

    [Fact]
    public async Task Activar_HappyPath_RetornaMensaje()
    {
        SetupAdminSesion();
        _lineas.Setup(l => l.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new LineaAyudaRow { IdLinea = 1, Nombre = "L1", Estado = "Inactiva" });
        _lineas.Setup(l => l.ActivarAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ActivarAsync("token", 1, "127.0.0.1", CancellationToken.None);

        result.Should().Contain("activada");
    }

    [Fact]
    public async Task Inactivar_HappyPath_RetornaMensaje()
    {
        SetupAdminSesion();
        _lineas.Setup(l => l.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new LineaAyudaRow { IdLinea = 1, Nombre = "L1", Estado = "Activa" });
        _lineas.Setup(l => l.InactivarAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.InactivarAsync("token", 1, "127.0.0.1", CancellationToken.None);

        result.Should().Contain("inactivada");
    }

    [Fact]
    public async Task Eliminar_HappyPath_RetornaMensaje()
    {
        SetupAdminSesion();
        _lineas.Setup(l => l.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new LineaAyudaRow { IdLinea = 1, Nombre = "L1", Estado = "Activa" });
        _lineas.Setup(l => l.EliminarAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.EliminarAsync("token", 1, "127.0.0.1", CancellationToken.None);

        result.Should().Contain("eliminada");
    }
}
