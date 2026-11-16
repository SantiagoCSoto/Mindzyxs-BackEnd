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

public class PruebasServicioRecuperacionContrasena
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICodigoRepository> _codes = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<INotificacionRepository> _notificacionesRepo = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<PasswordRecoveryService>> _logger = new();

    private PasswordRecoveryService CreateSut()
    {
        var notificaciones = new NotificationService(_notificacionesRepo.Object, _emailSender.Object, _authLogs.Object);
        var opts = Microsoft.Extensions.Options.Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 });
        return new PasswordRecoveryService(_users.Object, _codes.Object, _sesiones.Object, _authLogs.Object, notificaciones, opts, _logger.Object);
    }

    [Fact]
    public async Task Forgot_CorreoVacio_ReturnsFail()
    {
        var sut = CreateSut();
        var result = await sut.ForgotAsync("", CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Este campo es obligatorio");
    }

    [Fact]
    public async Task Forgot_CorreoInvalido_ReturnsFail()
    {
        var sut = CreateSut();
        var result = await sut.ForgotAsync("invalid", CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Ingresa un correo válido");
    }

    [Fact]
    public async Task Forgot_CorreoNoRegistrado_RetornaOkGenerico()
    {
        _users.Setup(u => u.FindByCorreoAsync("noexiste@test.com", It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.ForgotAsync("noexiste@test.com", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("recibirás un código");
    }

    [Fact]
    public async Task Forgot_CorreoRegistrado_GeneraCodigoYEnviaCorreo()
    {
        var user = new UsuarioDto { IdUsuario = 1, Nombre = "Camila", Correo = "camila@test.com" };
        _users.Setup(u => u.FindByCorreoAsync("camila@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.CrearAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _notificacionesRepo.Setup(n => n.FindByTipoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((NotificacionRow?)null);

        var sut = CreateSut();
        var result = await sut.ForgotAsync("camila@test.com", CancellationToken.None);

        result.Success.Should().BeTrue();
        _codes.Verify(c => c.CrearAsync(1, It.IsAny<string>(), "Recuperar", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task VerifyCode_CorreoVacio_ReturnsFail()
    {
        var sut = CreateSut();
        var result = await sut.VerifyCodeAsync("", "123456", CancellationToken.None);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task VerifyCode_UsuarioNoEncontrado_ReturnsFail()
    {
        _users.Setup(u => u.FindByCorreoAsync("noexiste@test.com", It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.VerifyCodeAsync("noexiste@test.com", "123456", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("incorrecto");
    }

    [Fact]
    public async Task VerifyCode_CodigoExpirado_ReturnsFail()
    {
        var user = new UsuarioDto { IdUsuario = 1, Correo = "u@test.com" };
        _users.Setup(u => u.FindByCorreoAsync("u@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.UltimoAsync(1, "Recuperar", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "123456", DateTime.UtcNow.AddMinutes(-5))));

        var sut = CreateSut();
        var result = await sut.VerifyCodeAsync("u@test.com", "123456", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expiró");
    }

    [Fact]
    public async Task VerifyCode_CodigoIncorrecto_ReturnsFail()
    {
        var user = new UsuarioDto { IdUsuario = 1, Correo = "u@test.com" };
        _users.Setup(u => u.FindByCorreoAsync("u@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.UltimoAsync(1, "Recuperar", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "654321", DateTime.UtcNow.AddMinutes(10))));

        var sut = CreateSut();
        var result = await sut.VerifyCodeAsync("u@test.com", "123456", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("incorrecto");
    }

    [Fact]
    public async Task VerifyCode_Valido_ReturnsOk()
    {
        var user = new UsuarioDto { IdUsuario = 1, Correo = "u@test.com" };
        _users.Setup(u => u.FindByCorreoAsync("u@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.UltimoAsync(1, "Recuperar", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "123456", DateTime.UtcNow.AddMinutes(10))));

        var sut = CreateSut();
        var result = await sut.VerifyCodeAsync("u@test.com", "123456", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("verificado");
    }

    [Fact]
    public async Task ResetPassword_UsuarioNoEncontrado_ReturnsFail()
    {
        _users.Setup(u => u.FindByCorreoAsync("noexiste@test.com", It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.ResetPasswordAsync("noexiste@test.com", "123456", "Abc12345", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("incorrecto");
    }

    [Fact]
    public async Task ResetPassword_CodigoExpirado_ReturnsFail()
    {
        var user = new UsuarioDto { IdUsuario = 1, Correo = "u@test.com" };
        _users.Setup(u => u.FindByCorreoAsync("u@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.UltimoAsync(1, "Recuperar", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "123456", DateTime.UtcNow.AddMinutes(-5))));

        var sut = CreateSut();
        var result = await sut.ResetPasswordAsync("u@test.com", "123456", "Abc12345", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("expiró");
    }

    [Fact]
    public async Task ResetPassword_PasswordDebil_ReturnsFail()
    {
        var user = new UsuarioDto { IdUsuario = 1, Correo = "u@test.com" };
        _users.Setup(u => u.FindByCorreoAsync("u@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.UltimoAsync(1, "Recuperar", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "123456", DateTime.UtcNow.AddMinutes(10))));

        var sut = CreateSut();
        var result = await sut.ResetPasswordAsync("u@test.com", "123456", "weak", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("contraseña debe");
    }

    [Fact]
    public async Task ResetPassword_Valido_ActualizaPasswordYRevocaSesiones()
    {
        var user = new UsuarioDto { IdUsuario = 1, Correo = "u@test.com" };
        _users.Setup(u => u.FindByCorreoAsync("u@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.UltimoAsync(1, "Recuperar", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "123456", DateTime.UtcNow.AddMinutes(10))));
        _users.Setup(u => u.SetPasswordAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _codes.Setup(c => c.ConsumirAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _sesiones.Setup(s => s.DeleteByUsuarioAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ResetPasswordAsync("u@test.com", "123456", "Abc12345", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("actualizada");
        _users.Verify(u => u.SetPasswordAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);
        _sesiones.Verify(s => s.DeleteByUsuarioAsync(1, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPassword_ErrorInterno_ReturnsFail()
    {
        var user = new UsuarioDto { IdUsuario = 1, Correo = "u@test.com" };
        _users.Setup(u => u.FindByCorreoAsync("u@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.UltimoAsync(1, "Recuperar", It.IsAny<CancellationToken>())).Returns(
            Task.FromResult<mindzyxsCode?>(new mindzyxsCode(1, "123456", DateTime.UtcNow.AddMinutes(10))));
        _users.Setup(u => u.SetPasswordAsync(1, It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));

        var sut = CreateSut();
        var result = await sut.ResetPasswordAsync("u@test.com", "123456", "Abc12345", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No fue posible");
    }

    [Fact]
    public async Task Resend_UsuarioNoEncontrado_ReturnsOkGenerico()
    {
        _users.Setup(u => u.FindByCorreoAsync("noexiste@test.com", It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.ResendAsync("noexiste@test.com", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("reenviado");
    }

    [Fact]
    public async Task Resend_UsuarioExistente_CreaCodigoYEnviaCorreo()
    {
        var user = new UsuarioDto { IdUsuario = 1, Nombre = "Camila", Correo = "camila@test.com" };
        _users.Setup(u => u.FindByCorreoAsync("camila@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _codes.Setup(c => c.CrearAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificacionesRepo.Setup(n => n.FindByTipoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((NotificacionRow?)null);
        _emailSender.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ResendAsync("camila@test.com", CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Message.Should().Contain("reenviado");
        _codes.Verify(c => c.CrearAsync(1, It.IsAny<string>(), "Recuperar", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}

