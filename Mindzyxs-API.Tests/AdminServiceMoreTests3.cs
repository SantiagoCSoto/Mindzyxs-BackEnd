using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AdminServiceMoreTests3
{
    private readonly Mock<IAdminRepository> _admin = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<INotificacionRepository> _notificaciones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<LoginService> _login = new();
    private readonly Mock<IEmailSender> _email = new();
    private readonly Mock<ITarjetaStorage> _tarjetas = new();
    private readonly Mock<Microsoft.Extensions.Options.IOptions<AdminOptions>> _adminOptions = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<AdminService>> _logger = new();

    private AdminService CreateSut()
    {
        _adminOptions.Setup(o => o.Value).Returns(new AdminOptions { BackupsPath = "C:\\temp\\backups", RetencionLogDias = 90 });
        var notifRepo = new Mock<INotificacionRepository>();
        var recovery = new PasswordRecoveryService(_users.Object, new Mock<ICodigoRepository>().Object, new Mock<ISesionRepository>().Object, _authLogs.Object, new NotificationService(notifRepo.Object, _email.Object, _authLogs.Object), Microsoft.Extensions.Options.Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 }), new Mock<Microsoft.Extensions.Logging.ILogger<PasswordRecoveryService>>().Object);
        var login = new LoginService(_users.Object, _catalogs.Object, new Mock<ISesionRepository>().Object, _authLogs.Object, Microsoft.Extensions.Options.Options.Create(new SesionOptions { ExpiraHoras = 8 }), new Mock<Microsoft.Extensions.Logging.ILogger<LoginService>>().Object);
        return new AdminService(_admin.Object, _users.Object, _catalogs.Object, _notificaciones.Object, _authLogs.Object, login, recovery, _email.Object, _tarjetas.Object, _adminOptions.Object, _logger.Object);
    }

    [Fact]
    public async Task CreateBackup_RetornaResultado()
    {
        _admin.Setup(a => a.CreateSqlBackupAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _admin.Setup(a => a.CrearRespaldoAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.CreateBackupAsync(1, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task RunMaintenance_RetornaResultado()
    {
        _admin.Setup(a => a.LimpiarTodoAsync(90, It.IsAny<CancellationToken>())).ReturnsAsync(new MantenimientoLimpiezaResult(5, 3, 2));
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.RunMaintenanceAsync(1, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DetalleValidacion_NoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.DetalleValidacionAsync(99, CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task CambiarEstado_UsuarioNoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.CambiarEstadoAsync(1, 99, "Suspendida", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Usuario no encontrado.");
    }

    [Fact]
    public async Task ForzarReset_UsuarioNoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.ForzarResetAsync(1, 99, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Usuario no encontrado.");
    }

    [Fact]
    public async Task TarjetaPath_NoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.TarjetaPathAsync(99, CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task TarjetaStream_NoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.TarjetaStreamAsync(99, CancellationToken.None);

        result.Success.Should().BeFalse();
    }
}

