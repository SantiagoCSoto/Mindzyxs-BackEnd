using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioAuth
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ICodigoRepository> _codes = new();
    private readonly Mock<ITarjetaStorage> _tarjetas = new();
    private readonly Mock<INotificacionRepository> _notificacionesRepo = new();
    private readonly Mock<IEmailSender> _emailSender = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private NotificationService _notificaciones => new(_notificacionesRepo.Object, _emailSender.Object, _authLogs.Object);
    private readonly Mock<Microsoft.Extensions.Options.IOptions<RegistroOptions>> _registroOptions = new();
    private readonly Mock<Microsoft.Extensions.Logging.ILogger<AuthService>> _logger = new();

    private AuthService CreateSut()
    {
        _registroOptions.Setup(o => o.Value).Returns(new RegistroOptions { CodigoExpiraMinutos = 15 });
        return new AuthService(_users.Object, _catalogs.Object, _codes.Object, _tarjetas.Object,
            _notificaciones, _registroOptions.Object, _logger.Object);
    }

    [Fact]
    public async Task Register_NullVm_ReturnsFail()
    {
        var sut = CreateSut();
        var result = await sut.RegisterAsync(null!, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Solicitud");
    }

    [Fact]
    public async Task Register_InvalidTipoUsuario_ReturnsFail()
    {
        var vm = new RegisterVm { TipoUsuario = "Medico", Nombre = "Test", Apellido = "User", Correo = "test@test.com", Password = "Abc12345" };
        var sut = CreateSut();
        var result = await sut.RegisterAsync(vm, CancellationToken.None);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Register_WeakPassword_ReturnsFail()
    {
        var vm = new RegisterVm { TipoUsuario = "Paciente", Nombre = "Test", Apellido = "User", Correo = "test@test.com", Password = "abc" };
        var sut = CreateSut();
        var result = await sut.RegisterAsync(vm, CancellationToken.None);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Register_CorreoDuplicado_ReturnsFail()
    {
        _catalogs.Setup(c => c.ExisteGeneroAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _catalogs.Setup(c => c.GetRolIdAsync("Paciente", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _catalogs.Setup(c => c.GetEstadoIdAsync("PendienteVerificacion", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _users.Setup(u => u.ExistsByCorreoAsync("dup@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var vm = new RegisterVm { TipoUsuario = "Paciente", Nombre = "Test", Apellido = "User", Correo = "dup@test.com", Password = "Abc12345", IdGenero = 1, FechaNacimiento = "2000-01-01" };
        var sut = CreateSut();
        var result = await sut.RegisterAsync(vm, CancellationToken.None);
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("registrado");
    }

    [Fact]
    public async Task Register_CorreoInvalido_ReturnsFail()
    {
        var vm = new RegisterVm { TipoUsuario = "Paciente", Nombre = "Test", Apellido = "User", Correo = "invalid", Password = "Abc12345" };
        var sut = CreateSut();
        var result = await sut.RegisterAsync(vm, CancellationToken.None);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Register_NombreVacio_ReturnsFail()
    {
        var vm = new RegisterVm { TipoUsuario = "Paciente", Nombre = "", Apellido = "User", Correo = "test@test.com", Password = "Abc12345" };
        var sut = CreateSut();
        var result = await sut.RegisterAsync(vm, CancellationToken.None);
        result.Success.Should().BeFalse();
    }

    [Fact]
    public async Task Register_PacienteValido_RetornaOk()
    {
        _catalogs.Setup(c => c.ExisteGeneroAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _catalogs.Setup(c => c.GetRolIdAsync("Paciente", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _catalogs.Setup(c => c.GetEstadoIdAsync("PendienteVerificacion", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _users.Setup(u => u.ExistsByCorreoAsync("pac@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(u => u.AddAsync(It.IsAny<UsuarioDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(42);
        _codes.Setup(c => c.CrearAsync(42, It.IsAny<string>(), "Registro", It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _tarjetas.Setup(t => t.Validate(It.IsAny<IFormFile>(), It.IsAny<bool>(), out It.Ref<string?>.IsAny)).Returns((string?)null);
        _notificacionesRepo.Setup(n => n.FindByTipoAsync("CODE_SENT", It.IsAny<CancellationToken>())).ReturnsAsync((NotificacionRow?)null);
        _emailSender.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var vm = new RegisterVm { TipoUsuario = "Paciente", Nombre = "Ana", Apellido = "Paz", Correo = "pac@test.com", Password = "Abc12345", IdGenero = 1, FechaNacimiento = "2000-01-01" };
        var sut = CreateSut();
        var result = await sut.RegisterAsync(vm, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(42);
    }

    [Fact]
    public async Task Register_PsicologoValido_ConTarjeta_RetornaOk()
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("tarjeta.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(100);
        _catalogs.Setup(c => c.ExisteGeneroAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _catalogs.Setup(c => c.ExisteEspecialidadAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _catalogs.Setup(c => c.GetRolIdAsync("Psicologo", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _catalogs.Setup(c => c.GetEstadoIdAsync("PendienteVerificacion", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _catalogs.Setup(c => c.GetEstadoIdAsync("PendienteAprobacion", It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _users.Setup(u => u.ExistsByCorreoAsync("psi@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(u => u.AddAsync(It.IsAny<UsuarioDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(7);
        _users.Setup(u => u.GuardarEspecialidadesAsync(7, It.IsAny<List<int>>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _tarjetas.Setup(t => t.Validate(It.IsAny<IFormFile>(), true, out It.Ref<string?>.IsAny)).Returns((string?)null);
        _tarjetas.Setup(t => t.SaveAsync(It.IsAny<IFormFile>(), 7, It.IsAny<CancellationToken>())).ReturnsAsync("tarjetas/7.pdf");
        _users.Setup(u => u.SetFilePathAsync(7, It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _codes.Setup(c => c.CrearAsync(7, It.IsAny<string>(), "Registro", It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _notificacionesRepo.Setup(n => n.FindByTipoAsync("CODE_SENT", It.IsAny<CancellationToken>())).ReturnsAsync((NotificacionRow?)null);
        _emailSender.Setup(e => e.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var vm = new RegisterVm { TipoUsuario = "Psicologo", Nombre = "Laura", Apellido = "Gomez", Correo = "psi@test.com", Password = "Abc12345", IdGenero = 1, FechaNacimiento = "1990-01-01", Especialidades = new List<int> { 1 }, TarjetaProfesional = fileMock.Object };
        var sut = CreateSut();
        var result = await sut.RegisterAsync(vm, CancellationToken.None);

        result.Success.Should().BeTrue();
        result.Data.Should().Be(7);
    }
}

