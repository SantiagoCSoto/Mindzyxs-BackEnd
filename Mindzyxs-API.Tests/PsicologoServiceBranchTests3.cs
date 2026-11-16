using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;
using Microsoft.AspNetCore.Http;

namespace Mindzyxs_API.Tests.Services;

public class PsicologoServiceBranchTests3
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<ITarjetaStorage> _tarjetas = new();

    private PsicologoService CreateSut() => new(
        _users.Object, _catalogs.Object, _sesiones.Object, _authLogs.Object, _tarjetas.Object);

    private void SetupSesion(int userId = 5, string estado = "Activa", string aprob = "Aprobado")
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1, Correo = "psi@test.com", Nombre = "Laura", Apellido = "Gomez" };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(estado);
        _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(aprob);
    }

    [Fact]
    public async Task ObtenerPerfilProfesional_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.ObtenerPerfilProfesionalAsync(null, CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ObtenerPerfilProfesional_NoEncontrado_Lanza()
    {
        SetupSesion();
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var act = () => sut.ObtenerPerfilProfesionalAsync("token", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task ObtenerPerfilProfesional_Valido_RetornaPerfil()
    {
        SetupSesion();
        var perfil = new UsuarioDto
        {
            IdUsuario = 5, Nombre = "Laura", Apellido = "Gomez", Correo = "psi@test.com",
            AñosExperiencia = 10, DescripcionProfesional = "Desc", DatosContacto = "tel",
            TarjetaProfesionalVerificada = true, FilePath = "path.pdf",
            Especialidades = new List<string> { "Ansiedad" }, EspecialidadesPendientes = new List<string>()
        };
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(perfil);

        var sut = CreateSut();
        var result = await sut.ObtenerPerfilProfesionalAsync("token", CancellationToken.None);

        result.Id.Should().Be(5);
        result.TieneTarjetaArchivo.Should().BeTrue();
    }

    [Fact]
    public async Task Directorio_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.DirectorioAsync(null, null, null, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task SubirTarjeta_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.SubirTarjetaAsync(null, null, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ActualizarPerfil_AniosNegativos_Lanza()
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new ActualizarPerfilProfesionalVm { AñosExperiencia = -1 };
        var act = () => sut.ActualizarPerfilProfesionalAsync("token", vm, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*negativos*");
    }

    [Fact]
    public async Task ActualizarPerfil_CorreoInvalido_Lanza()
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new ActualizarPerfilProfesionalVm { Correo = "invalid" };
        var act = () => sut.ActualizarPerfilProfesionalAsync("token", vm, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*correo válido*");
    }

    [Fact]
    public async Task ActualizarPerfil_CorreoDuplicado_Lanza()
    {
        SetupSesion();
        _users.Setup(u => u.ExistsByCorreoAsync("dup@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();
        var vm = new ActualizarPerfilProfesionalVm { Correo = "dup@test.com" };
        var act = () => sut.ActualizarPerfilProfesionalAsync("token", vm, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*ya se encuentra registrado*");
    }

    [Fact]
    public async Task ActualizarPerfil_EspecialidadInexistente_Lanza()
    {
        SetupSesion();
        _catalogs.Setup(c => c.ExisteEspecialidadAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        var vm = new ActualizarPerfilProfesionalVm { Especialidades = new List<int> { 999 } };
        var act = () => sut.ActualizarPerfilProfesionalAsync("token", vm, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no existe*");
    }

    [Fact]
    public async Task ActualizarPerfil_Valido_RetornaMensaje()
    {
        SetupSesion();
        _users.Setup(u => u.ActualizarPerfilProfesionalAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new ActualizarPerfilProfesionalVm { AñosExperiencia = 10, DescripcionProfesional = "Psicólogo clínico" };
        var result = await sut.ActualizarPerfilProfesionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.Should().Contain("actualizada correctamente");
    }

    [Fact]
    public async Task SubirTarjeta_TarjetaInvalida_Lanza()
    {
        SetupSesion();
        var fileMock = new Mock<IFormFile>();
        _tarjetas.Setup(t => t.Validate(It.IsAny<IFormFile>(), true, out It.Ref<string?>.IsAny))
            .Returns((IFormFile f, bool r, out string? e) => { e = "El archivo debe estar en formato PDF"; return e; });

        var sut = CreateSut();
        var act = () => sut.SubirTarjetaAsync("token", fileMock.Object, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*formato PDF*");
    }

    [Fact]
    public async Task SubirTarjeta_Rechazado_ReabreSolicitud()
    {
        SetupSesion(5, "Inactiva", "Rechazado");
        var user = new UsuarioDto { IdUsuario = 5, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1, Correo = "psi@test.com", FilePath = null };
        _users.Setup(u => u.FindByIdAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(user);
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
