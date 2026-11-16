using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class NotificationServiceBranchTests
{
    private readonly Mock<INotificacionRepository> _notificaciones = new();
    private readonly Mock<IEmailSender> _email = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private NotificationService CreateSut() => new(_notificaciones.Object, _email.Object, _authLogs.Object);

    [Fact]
    public async Task EnviarCorreo_SinConfig_EnviaYRetornaTrue()
    {
        _notificaciones.Setup(n => n.FindByTipoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((NotificacionRow?)null);
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.EnviarCorreoAsync("CODE_SENT", "dest@test.com", new Dictionary<string, string> { ["nombre"] = "Ana" }, "subj", v => "body", 1, "127.0.0.1", CancellationToken.None);

        result.Should().BeTrue();
        _email.Verify(e => e.SendAsync("dest@test.com", "subj", "body", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnviarCorreo_ConfigInactiva_OmiteYRetornaFalse()
    {
        _notificaciones.Setup(n => n.FindByTipoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new NotificacionRow { Tipo = "CODE_SENT", Activo = false, Canales = "correo" });
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.EnviarCorreoAsync("CODE_SENT", "dest@test.com", new Dictionary<string, string>(), "subj", v => "body", 1, "127.0.0.1", CancellationToken.None);

        result.Should().BeFalse();
        _email.Verify(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EnviarCorreo_ConfigSinCanalCorreo_OmiteYRetornaFalse()
    {
        _notificaciones.Setup(n => n.FindByTipoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new NotificacionRow { Tipo = "CODE_SENT", Activo = true, Canales = "sms", Asunto = "a", Cuerpo = "b" });
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.EnviarCorreoAsync("CODE_SENT", "dest@test.com", new Dictionary<string, string>(), "subj", v => "body", 1, "127.0.0.1", CancellationToken.None);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task EnviarCorreo_ConfigConPlantilla_UsaPlantilla()
    {
        _notificaciones.Setup(n => n.FindByTipoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new NotificacionRow { Tipo = "CODE_SENT", Activo = true, Canales = "correo", Asunto = "Hola {nombre}", Cuerpo = "Codigo: {codigo}" });
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.EnviarCorreoAsync("CODE_SENT", "dest@test.com", new Dictionary<string, string> { ["nombre"] = "Ana", ["codigo"] = "123" }, "subj", v => "body", 1, "127.0.0.1", CancellationToken.None);

        result.Should().BeTrue();
        _email.Verify(e => e.SendAsync("dest@test.com", "Hola Ana", "Codigo: 123", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EnviarCorreo_ConfigAsuntoVacio_UsaFallback()
    {
        _notificaciones.Setup(n => n.FindByTipoAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(new NotificacionRow { Tipo = "CODE_SENT", Activo = true, Canales = "correo", Asunto = "  ", Cuerpo = "  " });
        _email.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.EnviarCorreoAsync("CODE_SENT", "dest@test.com", new Dictionary<string, string>(), "fallbackSubj", v => "fallbackBody", 1, "127.0.0.1", CancellationToken.None);

        result.Should().BeTrue();
        _email.Verify(e => e.SendAsync("dest@test.com", "fallbackSubj", "fallbackBody", It.IsAny<CancellationToken>()), Times.Once);
    }
}
