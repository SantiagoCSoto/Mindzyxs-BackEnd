using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioAdminLineasAyuda
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
    }

    [Fact]
    public async Task Crear_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var dto = new LineaAyudaAdminCrearDto { Nombre = "Test", Telefono = "123" };
        var act = () => sut.CrearAsync(null, dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Crear_NoAdmin_LanzaInvalidOperation()
    {
        var sesion = new SesionDto(1, 1, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = 1, IdRol = 1, IdEstadoCuenta = 1 };
        _users.Setup(u => u.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");

        var sut = CreateSut();
        var dto = new LineaAyudaAdminCrearDto { Nombre = "Test", Telefono = "123" };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*permisos*");
    }

    [Fact]
    public async Task Crear_NombreVacio_LanzaInvalidOperation()
    {
        SetupAdminSesion();
        var sut = CreateSut();
        var dto = new LineaAyudaAdminCrearDto { Nombre = "", Telefono = "123" };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*nombre y al menos un medio de contacto*");
    }

    [Fact]
    public async Task Crear_SinContacto_LanzaInvalidOperation()
    {
        SetupAdminSesion();
        var sut = CreateSut();
        var dto = new LineaAyudaAdminCrearDto { Nombre = "Test", Telefono = null, Url = null };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*nombre y al menos un medio de contacto*");
    }

    [Fact]
    public async Task ObtenerDetalle_NoEncontrado_LanzaInvalidOperation()
    {
        SetupAdminSesion();
        _lineas.Setup(l => l.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((LineaAyudaRow?)null);

        var sut = CreateSut();
        var act = () => sut.ObtenerDetalleAsync("token", 99, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Línea de ayuda no encontrada*");
    }

    [Fact]
    public async Task Eliminar_NoEncontrado_LanzaInvalidOperation()
    {
        SetupAdminSesion();
        _lineas.Setup(l => l.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((LineaAyudaRow?)null);

        var sut = CreateSut();
        var act = () => sut.EliminarAsync("token", 99, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Línea de ayuda no encontrada*");
    }
}

