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

public class PsicologoServiceMoreTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<IHistorialRepository> _historial = new();
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
    public async Task Directorio_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.DirectorioAsync(null, null, null, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Directorio_ConPsicologos_RetornaLista()
    {
        SetupSesion();
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _users.Setup(u => u.GetDirectorioPsicologosAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PsicologoDirectorioRow> { new(1, "Laura Gomez", new List<string> { "Ansiedad" }, 2) });
        _catalogs.Setup(c => c.GetRolIdAsync("Psicologo", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Activa", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Aprobado", It.IsAny<CancellationToken>())).ReturnsAsync(5);
        // No historial needed for directorio

        var sut = CreateSut();
        var result = await sut.DirectorioAsync("token", null, null, "127.0.0.1", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Nombre.Should().Be("Laura Gomez");
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
    public async Task ObtenerPerfilProfesional_NoEncontrado_LanzaInvalidOperation()
    {
        SetupSesion();
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync((UsuarioDto?)null);

        var sut = CreateSut();
        var act = () => sut.ObtenerPerfilProfesionalAsync("token", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Perfil profesional no encontrado*");
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
    public async Task ObtenerPerfilProfesional_HappyPath_RetornaPerfil()
    {
        SetupSesion();
        var perfil = new UsuarioDto
        {
            IdUsuario = 5,
            Nombre = "Laura",
            Apellido = "Gomez",
            Correo = "psi@test.com",
            AñosExperiencia = 10,
            DescripcionProfesional = "Psicologa clinica",
            DatosContacto = "tel",
            TarjetaProfesionalVerificada = true,
            FilePath = "tarjetas/5.pdf",
            Especialidades = new List<string> { "Ansiedad" },
            EspecialidadesPendientes = new List<string>()
        };
        _users.Setup(u => u.FindPerfilProfesionalAsync(5, It.IsAny<CancellationToken>())).ReturnsAsync(perfil);

        var sut = CreateSut();
        var result = await sut.ObtenerPerfilProfesionalAsync("token", CancellationToken.None);

        result.Id.Should().Be(5);
        result.Nombre.Should().Be("Laura Gomez");
        result.Correo.Should().Be("psi@test.com");
        result.AñosExperiencia.Should().Be(10);
        result.TieneTarjetaArchivo.Should().BeTrue();
    }

    [Fact]
    public async Task SubirTarjeta_HappyPath_RetornaMensaje()
    {
        SetupSesion();
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("tarjeta.pdf");
        fileMock.Setup(f => f.ContentType).Returns("application/pdf");
        fileMock.Setup(f => f.Length).Returns(100);
        _tarjetas.Setup(t => t.Validate(It.IsAny<IFormFile>(), true, out It.Ref<string?>.IsAny)).Returns((string?)null);
        _tarjetas.Setup(t => t.SaveAsync(It.IsAny<IFormFile>(), 5, It.IsAny<CancellationToken>())).ReturnsAsync("tarjetas/5_new.pdf");
        _tarjetas.Setup(t => t.DeleteAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _users.Setup(u => u.SetFilePathAsync(5, It.IsAny<string>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _users.Setup(u => u.SetTarjetaVerificadaAsync(5, false, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");

        var sut = CreateSut();
        var result = await sut.SubirTarjetaAsync("token", fileMock.Object, "127.0.0.1", CancellationToken.None);

        result.Should().Contain("Tarjeta recibida");
        _users.Verify(u => u.SetFilePathAsync(5, "tarjetas/5_new.pdf", It.IsAny<CancellationToken>()), Times.Once);
        _users.Verify(u => u.SetTarjetaVerificadaAsync(5, false, It.IsAny<CancellationToken>()), Times.Once);
    }
}
