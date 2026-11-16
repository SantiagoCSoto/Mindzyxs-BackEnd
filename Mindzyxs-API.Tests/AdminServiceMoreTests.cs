using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AdminServiceMoreTests2
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
    public async Task GetAdminId_SinSesion_RetornaNull()
    {
        var sesiones = new Mock<ISesionRepository>();
        sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        // AdminService.GetAdminIdAsync uses sesiones internally but we can't inject easily
        // This test verifies the method exists and is callable
        var sut = CreateSut();
        var result = await sut.GetAdminIdAsync(null, "127.0.0.1", CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task PendientesValidacion_RetornaLista()
    {
        _catalogs.Setup(c => c.GetRolIdAsync("Psicologo", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _catalogs.Setup(c => c.GetEstadoIdAsync("PendienteAprobacion", It.IsAny<CancellationToken>())).ReturnsAsync(4);
        _users.Setup(u => u.GetPsicologosPendientesAsync(2, 4, It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<PsicologoPendienteRow>()));

        var sut = CreateSut();
        var result = await sut.PendientesValidacionAsync(CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().BeEmpty();
    }

    [Fact]
    public async Task PendientesTarjeta_RetornaLista()
    {
        _catalogs.Setup(c => c.GetRolIdAsync("Psicologo", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _users.Setup(u => u.GetPsicologosTarjetaPendienteAsync(2, It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<PsicologoTarjetaPendienteRow>()));

        var sut = CreateSut();
        var result = await sut.PendientesTarjetaAsync(CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task TarjetaStream_NoEncontrado_RetornaFail()
    {
        _users.Setup(u => u.FindByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var result = await sut.TarjetaStreamAsync(99, CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task ActualizarNotificacion_TipoInvalido_RetornaFail()
    {
        var sut = CreateSut();
        var vm = new ActualizarNotificacionVm { Activo = true, Canales = new List<string> { "correo" }, Asunto = "Test", Cuerpo = "Body" };
        var result = await sut.ActualizarNotificacionAsync(1, "INVALID", vm, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Evento no encontrado.");
    }

    [Fact]
    public async Task ActualizarNotificacion_ActivoSinCanales_RetornaFail()
    {
        var sut = CreateSut();
        var vm = new ActualizarNotificacionVm { Activo = true, Canales = new List<string>(), Asunto = "Test", Cuerpo = "Body" };
        var result = await sut.ActualizarNotificacionAsync(1, "CODE_SENT", vm, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Debes habilitar al menos un canal para este evento.");
    }

    [Fact]
    public async Task ActualizarNotificacion_ActivoSinCuerpo_RetornaFail()
    {
        var sut = CreateSut();
        var vm = new ActualizarNotificacionVm { Activo = true, Canales = new List<string> { "correo" }, Asunto = "Test", Cuerpo = "" };
        var result = await sut.ActualizarNotificacionAsync(1, "CODE_SENT", vm, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("La plantilla no puede estar vacía.");
    }

    [Fact]
    public async Task ActualizarNotificacion_ActivoSinAsunto_RetornaFail()
    {
        var sut = CreateSut();
        var vm = new ActualizarNotificacionVm { Activo = true, Canales = new List<string> { "correo" }, Asunto = "", Cuerpo = "Body" };
        var result = await sut.ActualizarNotificacionAsync(1, "CODE_SENT", vm, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("El asunto es obligatorio para el canal correo.");
    }

    [Fact]
    public async Task PreviewNotificacion_TipoInvalido_RetornaFail()
    {
        var sut = CreateSut();
        var vm = new PreviewNotificacionVm { Nombre = "Test", Codigo = "123456" };
        var result = await sut.PreviewNotificacionAsync("INVALID", vm, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Evento no encontrado.");
    }

    [Fact]
    public async Task PreviewNotificacion_NoEncontrado_RetornaFail()
    {
        _notificaciones.Setup(n => n.FindByTipoAsync("CODE_SENT", It.IsAny<CancellationToken>())).ReturnsAsync((NotificacionRow?)null);

        var sut = CreateSut();
        var vm = new PreviewNotificacionVm { Nombre = "Test", Codigo = "123456" };
        var result = await sut.PreviewNotificacionAsync("CODE_SENT", vm, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Evento no encontrado.");
    }

    [Fact]
    public async Task Notificaciones_RetornaLista()
    {
        _notificaciones.Setup(n => n.GetAllAsync(It.IsAny<CancellationToken>())).Returns(Task.FromResult(new List<NotificacionRow>()));

        var sut = CreateSut();
        var result = await sut.NotificacionesAsync(CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task DetalleNotificacion_NoEncontrado_RetornaFail()
    {
        _notificaciones.Setup(n => n.FindByTipoAsync("INVALID", It.IsAny<CancellationToken>())).Returns(Task.FromResult<NotificacionRow?>(null));

        var sut = CreateSut();
        var result = await sut.DetalleNotificacionAsync("INVALID", CancellationToken.None);

        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Usuarios_RetornaLista()
    {
        _users.Setup(u => u.GetUsuariosAsync(It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<UsuarioAdminRow>());

        var sut = CreateSut();
        var result = await sut.UsuariosAsync(null, null, null, CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ListBackups_RetornaLista()
    {
        _admin.Setup(a => a.ListRespaldosAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<RespaldoDto>());

        var sut = CreateSut();
        var result = await sut.ListBackupsAsync(1, "127.0.0.1", CancellationToken.None);

        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task GetBackupFilePath_NoEncontrado_RetornaNull()
    {
        _admin.Setup(a => a.FindRespaldoByIdAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((RespaldoDto?)null);

        var sut = CreateSut();
        var result = await sut.GetBackupFilePath(99, CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task TarjetaStream_Valido_RetornaStream()
    {
        var perfil = new UsuarioDto { IdUsuario = 5, FilePath = "tarjetas/5.pdf" };
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(perfil);
        var ms = new MemoryStream(new byte[] { 1, 2, 3 });
        _tarjetas.Setup(t => t.OpenReadAsync("tarjetas/5.pdf", It.IsAny<CancellationToken>())).ReturnsAsync(ms);

        var sut = CreateSut();
        var result = await sut.TarjetaStreamAsync(5, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().NotBeNull();
        result.Data!.Length.Should().Be(3);
    }

    [Fact]
    public async Task TarjetaStream_FileNotFound_RetornaFail()
    {
        var perfil = new UsuarioDto { IdUsuario = 5, FilePath = "tarjetas/5.pdf" };
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(perfil);
        _tarjetas.Setup(t => t.OpenReadAsync("tarjetas/5.pdf", It.IsAny<CancellationToken>())).ThrowsAsync(new FileNotFoundException());

        var sut = CreateSut();
        var result = await sut.TarjetaStreamAsync(5, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("No se ha adjuntado");
    }
}
