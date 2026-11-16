using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AdminLineasAyudaServiceMoreTests2
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
    public async Task Listar_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.ListarAsync(null, null, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ObtenerDetalle_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.ObtenerDetalleAsync(null, 1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
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
    public async Task Activar_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.ActivarAsync(null, 1, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Inactivar_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.InactivarAsync(null, 1, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Eliminar_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.EliminarAsync(null, 1, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Actualizar_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var dto = new LineaAyudaAdminCrearDto { Nombre = "Test", Telefono = "123" };
        var act = () => sut.ActualizarAsync(null, 1, dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Actualizar_NoEncontrado_LanzaInvalidOperation()
    {
        SetupAdminSesion();
        _lineas.Setup(l => l.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((LineaAyudaRow?)null);

        var sut = CreateSut();
        var dto = new LineaAyudaAdminCrearDto { Nombre = "Test", Telefono = "123" };
        var act = () => sut.ActualizarAsync("token", 99, dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Línea de ayuda no encontrada*");
    }

    [Fact]
    public async Task Activar_NoEncontrado_LanzaInvalidOperation()
    {
        SetupAdminSesion();
        _lineas.Setup(l => l.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((LineaAyudaRow?)null);

        var sut = CreateSut();
        var act = () => sut.ActivarAsync("token", 99, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Línea de ayuda no encontrada*");
    }

    [Fact]
    public async Task Inactivar_NoEncontrado_LanzaInvalidOperation()
    {
        SetupAdminSesion();
        _lineas.Setup(l => l.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((LineaAyudaRow?)null);

        var sut = CreateSut();
        var act = () => sut.InactivarAsync("token", 99, "127.0.0.1", CancellationToken.None);

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
