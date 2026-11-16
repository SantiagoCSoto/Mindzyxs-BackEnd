using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioLogin
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<LoginService>> _logger = new();

    private LoginService CreateSut()
    {
        var opts = Microsoft.Extensions.Options.Options.Create(new SesionOptions { ExpiraHoras = 8 });
        return new LoginService(_users.Object, _catalogs.Object, _sesiones.Object, _authLogs.Object, opts, _logger.Object);
    }

    [Fact]
    public async Task Login_CorreoVacio_ReturnsFail()
    {
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.LoginAsync("", "Abc12345", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Este campo es obligatorio");
    }

    [Fact]
    public async Task Login_CorreoInvalido_ReturnsFail()
    {
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.LoginAsync("invalid", "Abc12345", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Ingresa un correo válido");
    }

    [Fact]
    public async Task Login_PasswordVacio_ReturnsFail()
    {
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.LoginAsync("test@test.com", "", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Este campo es obligatorio");
    }

    [Fact]
    public async Task Login_UsuarioNoEncontrado_ReturnsFail()
    {
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _users.Setup(u => u.FindByCorreoAsync("noexiste@test.com", It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.LoginAsync("noexiste@test.com", "Abc12345", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Correo o contraseña incorrectos");
    }

    [Fact]
    public async Task Login_CuentaSuspendida_ReturnsFail()
    {
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var user = new UsuarioDto { IdUsuario = 1, IdRol = 1, IdEstadoCuenta = 2, Correo = "susp@test.com", PasswordHash = "hash" };
        _users.Setup(u => u.FindByCorreoAsync("susp@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync("Suspendida");
        _users.Setup(u => u.GetRolNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Suspendida");

        var sut = CreateSut();
        var result = await sut.LoginAsync("susp@test.com", "Abc12345", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("suspendida");
    }

    [Fact]
    public async Task Login_CuentaPendienteVerificacion_ReturnsFail()
    {
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        var user = new UsuarioDto { IdUsuario = 2, IdRol = 1, IdEstadoCuenta = 3, Correo = "pend@test.com", PasswordHash = "hash" };
        _users.Setup(u => u.FindByCorreoAsync("pend@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync("PendienteVerificacion");
        _users.Setup(u => u.GetRolNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("PendienteVerificacion");

        var sut = CreateSut();
        var result = await sut.LoginAsync("pend@test.com", "Abc12345", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("suspendida");
    }

    [Fact]
    public async Task ValidateSession_TokenVacio_ReturnsFail()
    {
        var sut = CreateSut();
        var result = await sut.ValidateSessionAsync(null, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expirado");
    }

    [Fact]
    public async Task ValidateSession_SesionNoEncontrada_ReturnsFail()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var result = await sut.ValidateSessionAsync("token", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expirado");
    }

    [Fact]
    public async Task ValidateSession_Expirada_EliminaSesionYReturnsFail()
    {
        var sesion = new SesionDto(1, 5, "hash", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        _sesiones.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ValidateSessionAsync("token", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expirado");
        _sesiones.Verify(s => s.DeleteAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ValidateSession_UsuarioNoEncontrado_ReturnsFail()
    {
        var sesion = new SesionDto(1, 5, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.ValidateSessionAsync("token", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expirado");
    }

    [Fact]
    public async Task ValidateSession_EstadoNoActivo_EliminaSesionYReturnsFail()
    {
        var sesion = new SesionDto(1, 5, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 1, IdEstadoCuenta = 2, Correo = "u@test.com" };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync("Suspendida");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync("Suspendida");
        _sesiones.Setup(s => s.DeleteAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ValidateSessionAsync("token", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("suspendida");
    }

    [Fact]
    public async Task ValidateSession_Valida_ReturnsOk()
    {
        var sesion = new SesionDto(1, 5, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 1, IdEstadoCuenta = 1, Correo = "u@test.com" };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ValidateSessionAsync("token", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("válida");
        result.Data.Should().NotBeNull();
    }

    [Fact]
    public async Task Logout_SinToken_ReturnsOk()
    {
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.LogoutAsync(null, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("cerrada exitosamente");
    }

    [Fact]
    public async Task Logout_SesionInexistente_ReturnsOk()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.LogoutAsync("token", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task Logout_Valido_EliminaSesionYReturnsOk()
    {
        var sesion = new SesionDto(1, 5, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        _sesiones.Setup(s => s.DeleteByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.LogoutAsync("token", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
        _sesiones.Verify(s => s.DeleteByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Logout_Error_ReturnsFail()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Throws(new Exception("boom"));

        var sut = CreateSut();
        var result = await sut.LogoutAsync("token", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No fue posible cerrar sesión");
    }
}

