using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioVerificacion
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ICodigoRepository> _codes = new();
    private readonly Mock<INotificacionRepository> _notificacionesRepo = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private VerificationService CreateSut()
    {
        var notificaciones = new NotificationService(_notificacionesRepo.Object, _emailSender.Object, _authLogs.Object);
        var opts = Microsoft.Extensions.Options.Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 });
        return new VerificationService(_users.Object, _catalogs.Object, _codes.Object, notificaciones, opts);
    }

    [Fact]
    public async Task Verify_UsuarioNoEncontrado_ReturnsFail()
    {
        _users.Setup(u => u.FindByCorreoAsync("noexiste@test.com", It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.VerifyAsync("noexiste@test.com", "123456", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("incorrecto");
    }

    [Fact]
    public async Task Verify_SinCodigoPrevio_ReturnsFail()
    {
        var user = new UsuarioDto { IdUsuario = 1, IdRol = 1 };
        _users.Setup(u => u.FindByCorreoAsync("test@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.UltimoAsync(1, "Registro", It.IsAny<CancellationToken>())).Returns(Task.FromResult<mindzyxsCode?>(null));

        var sut = CreateSut();
        var result = await sut.VerifyAsync("test@test.com", "123456", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("incorrecto");
    }

    [Fact]
    public async Task Verify_CodigoExpirado_ReturnsFail()
    {
        var user = new UsuarioDto { IdUsuario = 1, IdRol = 1 };
        _users.Setup(u => u.FindByCorreoAsync("test@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.UltimoAsync(1, "Registro", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "123456", DateTime.UtcNow.AddMinutes(-5))));

        var sut = CreateSut();
        var result = await sut.VerifyAsync("test@test.com", "123456", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expiró");
    }

    [Fact]
    public async Task Verify_CodigoIncorrecto_ReturnsFail()
    {
        var user = new UsuarioDto { IdUsuario = 1, IdRol = 1 };
        _users.Setup(u => u.FindByCorreoAsync("test@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.UltimoAsync(1, "Registro", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "654321", DateTime.UtcNow.AddMinutes(10))));

        var sut = CreateSut();
        var result = await sut.VerifyAsync("test@test.com", "123456", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("incorrecto");
    }

    [Fact]
    public async Task Verify_PacienteValido_ActivaCuenta()
    {
        var user = new UsuarioDto { IdUsuario = 1, IdRol = 1, Nombre = "Camila" };
        _users.Setup(u => u.FindByCorreoAsync("test@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.UltimoAsync(1, "Registro", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "123456", DateTime.UtcNow.AddMinutes(10))));
        _codes.Setup(c => c.ConsumirAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _catalogs.Setup(c => c.GetRolIdAsync("Psicologo", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Activa", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _users.Setup(u => u.SetEstadoCuentaAsync(1, 1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.VerifyAsync("test@test.com", "123456", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be("Activa");
    }

    [Fact]
    public async Task Resend_UsuarioNoEncontrado_RetornaOkGenerico()
    {
        _users.Setup(u => u.FindByCorreoAsync("noexiste@test.com", It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);
        _notificacionesRepo.Setup(n => n.FindByTipoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((NotificacionRow?)null);

        var sut = CreateSut();
        var result = await sut.ResendAsync("noexiste@test.com", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("recibirás un código");
    }

    [Fact]
    public async Task Verify_PsicologoValido_PendienteAprobacion()
    {
        var user = new UsuarioDto { IdUsuario = 2, IdRol = 2, Nombre = "Laura" };
        _users.Setup(u => u.FindByCorreoAsync("psi@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.UltimoAsync(2, "Registro", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "123456", DateTime.UtcNow.AddMinutes(10))));
        _codes.Setup(c => c.ConsumirAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _catalogs.Setup(c => c.GetRolIdAsync("Psicologo", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _catalogs.Setup(c => c.GetEstadoIdAsync("PendienteAprobacion", It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _users.Setup(u => u.SetEstadoCuentaAsync(2, 3, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.VerifyAsync("psi@test.com", "123456", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be("PendienteAprobacion");
    }

    [Fact]
    public async Task Resend_UsuarioExistente_ReenviaCodigoYRetornaOk()
    {
        var user = new UsuarioDto { IdUsuario = 1, Correo = "test@test.com", Nombre = "Camila" };
        _users.Setup(u => u.FindByCorreoAsync("test@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.CrearAsync(1, It.IsAny<string>(), "Registro", It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificacionesRepo.Setup(n => n.FindByTipoAsync("CODE_SENT", It.IsAny<CancellationToken>())).ReturnsAsync((NotificacionRow?)null);
        _emailSender.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ResendAsync("test@test.com", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("reenviado");
        _codes.Verify(c => c.CrearAsync(1, It.IsAny<string>(), "Registro", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

