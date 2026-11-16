using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioAccesos
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private AccesosService CreateSut() => new(
        _users.Object, _catalogs.Object, _sesiones.Object, _historial.Object, _authLogs.Object);

    private void SetupSesion(int userId = 1)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 1, IdEstadoCuenta = 1 };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
    }

    [Fact]
    public async Task Autorizar_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.AutorizarAsync(null, 5, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Autorizar_PsicologoNoEncontrado_LanzaInvalidOperation()
    {
        SetupSesion();
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var act = () => sut.AutorizarAsync("token", 5, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task Autorizar_PsicologoNoActivo_LanzaInvalidOperation()
    {
        SetupSesion();
        var psi = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 2 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(psi);
        _users.Setup(u => u.GetRolNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _users.Setup(u => u.GetRolNombreAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync("Inactiva");

        var sut = CreateSut();
        var act = () => sut.AutorizarAsync("token", 5, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no está activo*");
    }

    [Fact]
    public async Task Autorizar_EstadoAutorizadoNoConfigurado_LanzaInvalidOperation()
    {
        SetupSesion();
        var psi = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(psi);
        _users.Setup(u => u.GetRolNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _users.Setup(u => u.GetRolNombreAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);

        var sut = CreateSut();
        var act = () => sut.AutorizarAsync("token", 5, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Autorizado no configurado*");
    }

    [Fact]
    public async Task Autorizar_Valido_RetornaMensajeYRegistra()
    {
        SetupSesion();
        var psi = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(psi);
        _users.Setup(u => u.GetRolNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _users.Setup(u => u.GetRolNombreAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _historial.Setup(h => h.AsegurarAutorizacionAsync(1, 5, 8, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.AutorizarAsync("token", 5, "127.0.0.1", CancellationToken.None);

        result.Should().Be("Acceso autorizado correctamente");
    }

    [Fact]
    public async Task Revocar_PsicologoNoEncontrado_LanzaInvalidOperation()
    {
        SetupSesion();
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var act = () => sut.RevocarAsync("token", 5, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task Revocar_EstadoRevocadoNoConfigurado_LanzaInvalidOperation()
    {
        SetupSesion();
        var psi = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(psi);
        _historial.Setup(h => h.GetEstadoIdAsync("Revocado", It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);

        var sut = CreateSut();
        var act = () => sut.RevocarAsync("token", 5, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Revocado no configurado*");
    }

    [Fact]
    public async Task Revocar_Valido_RetornaMensajeYRegistra()
    {
        SetupSesion();
        var psi = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(psi);
        _historial.Setup(h => h.GetEstadoIdAsync("Revocado", It.IsAny<CancellationToken>())).ReturnsAsync(9);
        _historial.Setup(h => h.RevocarAutorizacionAsync(1, 5, 9, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.RevocarAsync("token", 5, "127.0.0.1", CancellationToken.None);

        result.Should().Be("Acceso revocado correctamente");
    }

    [Fact]
    public async Task MisAccesos_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.MisAccesosAsync(null, null, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task MisAccesos_SinPsicologos_RetornaListaVacia()
    {
        SetupSesion();
        _catalogs.Setup(c => c.GetRolIdAsync("Psicologo", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Activa", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Aprobado", It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _users.Setup(u => u.GetDirectorioPsicologosAsync(2, 1, 5, 8, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PsicologoDirectorioRow>());
        _historial.Setup(h => h.GetPsicologosAutorizadosAsync(1, 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.MisAccesosAsync("token", null, "127.0.0.1", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task MisAccesos_ConPsicologos_RetornaListaConEstadoAutorizacion()
    {
        SetupSesion();
        _catalogs.Setup(c => c.GetRolIdAsync("Psicologo", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Activa", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Aprobado", It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _users.Setup(u => u.GetDirectorioPsicologosAsync(2, 1, 5, 8, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PsicologoDirectorioRow> { new(1, "Laura Gomez", new List<string> { "Ansiedad" }, 2) });
        _historial.Setup(h => h.GetPsicologosAutorizadosAsync(1, 8, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<int> { 1 });
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.MisAccesosAsync("token", null, "127.0.0.1", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Autorizado.Should().BeTrue();
    }
}

