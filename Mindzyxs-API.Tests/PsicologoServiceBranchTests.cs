using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PsicologoServiceBranchTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<ITarjetaStorage> _tarjetas = new();

    private PsicologoService CreateSut() => new(
        _users.Object, _catalogs.Object, _sesiones.Object, _authLogs.Object, _tarjetas.Object);

    private void SetupSesion(int userId = 5)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1, Correo = "psi@test.com", Nombre = "Laura", Apellido = "Gomez" };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");
    }

    [Fact]
    public async Task ActualizarPerfil_CorreoValido_ActualizaCorreo()
    {
        SetupSesion();
        _users.Setup(u => u.ExistsByCorreoAsync("nuevo@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);
        _users.Setup(u => u.ActualizarPerfilProfesionalAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _users.Setup(u => u.ActualizarCorreoAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new ActualizarPerfilProfesionalVm { Correo = "nuevo@test.com" };
        var result = await sut.ActualizarPerfilProfesionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.Should().Contain("actualizada");
        _users.Verify(u => u.ActualizarCorreoAsync(5, "nuevo@test.com", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ActualizarPerfil_EspecialidadesCambian_RetornaMensajeOcultas()
    {
        SetupSesion();
        _users.Setup(u => u.ActualizarPerfilProfesionalAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new ActualizarPerfilProfesionalVm { Especialidades = new List<int> { 1 } };
        _catalogs.Setup(c => c.ExisteEspecialidadAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var result = await sut.ActualizarPerfilProfesionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.Should().Contain("ocultas");
    }

    [Fact]
    public async Task SubirTarjeta_TarjetaInvalida_Lanza()
    {
        SetupSesion();
        _tarjetas.Setup(t => t.Validate(It.IsAny<IFormFile>(), true, out It.Ref<string?>.IsAny))
            .Returns((IFormFile f, bool r, out string? e) => { e = "El archivo debe estar en formato PDF"; return e; });

        var sut = CreateSut();
        var fileMock = new Mock<IFormFile>();
        var act = () => sut.SubirTarjetaAsync("token", fileMock.Object, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*formato PDF*");
    }

    [Fact]
    public async Task SubirTarjeta_ConOldPath_EliminaAnterior()
    {
        SetupSesion();
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1, Correo = "psi@test.com", FilePath = "old/path.pdf" };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("tarjeta.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(100);
        _tarjetas.Setup(t => t.Validate(It.IsAny<IFormFile>(), true, out It.Ref<string?>.IsAny)).Returns((string?)null);
        _tarjetas.Setup(t => t.SaveAsync(It.IsAny<IFormFile>(), 5, It.IsAny<CancellationToken>())).ReturnsAsync("new/path.pdf");
        _tarjetas.Setup(t => t.DeleteAsync("old/path.pdf", It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _users.Setup(u => u.SetFilePathAsync(5, It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _users.Setup(u => u.SetTarjetaVerificadaAsync(5, false, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");

        var sut = CreateSut();
        var result = await sut.SubirTarjetaAsync("token", fileMock.Object, "127.0.0.1", CancellationToken.None);

        result.Should().Contain("Tarjeta recibida");
        _tarjetas.Verify(t => t.DeleteAsync("old/path.pdf", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubirTarjeta_Rechazado_ReabreSolicitud()
    {
        SetupSesion();
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1, Correo = "psi@test.com", FilePath = null };
        // Override: estado Inactiva for rechazado case
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Inactiva");
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _catalogs.Setup(c => c.GetEstadoNombreAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync("Rechazado");
        _catalogs.Setup(c => c.GetEstadoIdAsync(AccountStates.PendienteAprobacion, It.IsAny<CancellationToken>())).ReturnsAsync(4);
        _users.Setup(u => u.SetEstadoAprobacionAsync(5, 4, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("tarjeta.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(100);
        _tarjetas.Setup(t => t.Validate(It.IsAny<IFormFile>(), true, out It.Ref<string?>.IsAny)).Returns((string?)null);
        _tarjetas.Setup(t => t.SaveAsync(It.IsAny<IFormFile>(), 5, It.IsAny<CancellationToken>())).ReturnsAsync("new/path.pdf");
        _users.Setup(u => u.SetFilePathAsync(5, It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _users.Setup(u => u.SetTarjetaVerificadaAsync(5, false, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.SubirTarjetaAsync("token", fileMock.Object, "127.0.0.1", CancellationToken.None);

        result.Should().Contain("Tarjeta recibida");
        _users.Verify(u => u.SetEstadoAprobacionAsync(5, 4, It.IsAny<CancellationToken>()), Times.Once);
    }
}
