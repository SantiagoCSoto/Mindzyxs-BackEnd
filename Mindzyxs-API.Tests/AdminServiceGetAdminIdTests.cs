using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AdminServiceGetAdminIdTests
{
    private readonly Mock<IAdminRepository> _admin = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<INotificacionRepository> _notificaciones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IEmailSender> _email = new();
    private readonly Mock<ITarjetaStorage> _tarjetas = new();
    private readonly Mock<Microsoft.Extensions.Options.IOptions<AdminOptions>> _adminOptions = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<AdminService>> _logger = new();

    private AdminService CreateSut()
    {
        _adminOptions.Setup(o => o.Value).Returns(new AdminOptions { BackupsPath = "C:\\temp\\backups", RetencionLogDias = 90 });
        var notifRepo = new Mock<INotificacionRepository>();
        var recovery = new PasswordRecoveryService(_users.Object, new Mock<ICodigoRepository>().Object, _sesiones.Object, _authLogs.Object, new NotificationService(notifRepo.Object, _email.Object, _authLogs.Object), Microsoft.Extensions.Options.Options.Create(new RegistroOptions { CodigoExpiraMinutos = 15 }), new Mock<Microsoft.Extensions.Logging.ILogger<PasswordRecoveryService>>().Object);
        var login = new LoginService(_users.Object, _catalogs.Object, _sesiones.Object, _authLogs.Object, Microsoft.Extensions.Options.Options.Create(new SesionOptions { ExpiraHoras = 8 }), new Mock<Microsoft.Extensions.Logging.ILogger<LoginService>>().Object);
        return new AdminService(_admin.Object, _users.Object, _catalogs.Object, _notificaciones.Object, _authLogs.Object, login, recovery, _email.Object, _tarjetas.Object, _adminOptions.Object, _logger.Object);
    }

    [Fact]
    public async Task GetAdminId_SinSesion_RetornaNull()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.GetAdminIdAsync(null, "127.0.0.1", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAdminId_SesionValidaAdmin_RetornaId()
    {
        var sesion = new SesionDto(1, 99, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var adminUser = new UsuarioDto { IdUsuario = 99, IdRol = 3, IdEstadoCuenta = 1 };
        _users.Setup(u => u.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync(adminUser);
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _users.Setup(u => u.GetRolNombreAsync(3, It.IsAny<CancellationToken>())).ReturnsAsync("Administrador");
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.GetAdminIdAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().Be(99);
    }

    [Fact]
    public async Task GetAdminId_SesionValidaNoAdmin_RetornaNull()
    {
        var sesion = new SesionDto(1, 7, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = 7, IdRol = 1, IdEstadoCuenta = 1 };
        _users.Setup(u => u.FindByIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _users.Setup(u => u.GetRolNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.GetAdminIdAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAdminId_SesionExpirada_RetornaNull()
    {
        var sesion = new SesionDto(1, 99, "hash", DateTime.UtcNow.AddHours(-1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.GetAdminIdAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().BeNull();
    }
}
