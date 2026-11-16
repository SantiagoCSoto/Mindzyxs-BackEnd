using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioAdmin
{
    private readonly Mock<IAdminRepository> _admin = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<INotificacionRepository> _notificaciones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
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
    public async Task VerificarTarjeta_PsicologoNoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.VerificarTarjetaAsync(1, 99, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Psicólogo no encontrado.");
    }

    [Fact]
    public async Task VerificarTarjeta_NoEsPsicologo_RetornaFail()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 1 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");

        var sut = CreateSut();
        var result = await sut.VerificarTarjetaAsync(1, 5, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("El usuario no es psicólogo.");
    }

    [Fact]
    public async Task AprobarPsicologo_NoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.AprobarPsicologoAsync(1, 99, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Psicólogo no encontrado.");
    }

    [Fact]
    public async Task AprobarPsicologo_NoEsPsicologo_RetornaFail()
    {
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 1 };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");

        var sut = CreateSut();
        var result = await sut.AprobarPsicologoAsync(1, 5, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("El usuario no es psicólogo.");
    }

    [Fact]
    public async Task RechazarPsicologo_NoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.RechazarPsicologoAsync(1, 99, "motivo", "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Psicólogo no encontrado.");
    }

    [Fact]
    public async Task TarjetaPath_NoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.TarjetaPathAsync(99, CancellationToken.None);

        result.Success.Should().BeFalse();
    }
}

