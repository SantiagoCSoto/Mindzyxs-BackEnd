using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PsicologoServiceMoreTests2
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<ITarjetaStorage> _tarjetas = new();

    private PsicologoService CreateSut() => new(
        _users.Object, _catalogs.Object, _sesiones.Object, _authLogs.Object, _tarjetas.Object);

    private void SetupSesion(int userId = 5, string rol = "Psicologo")
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = rol == "Psicologo" ? 2 : 1, IdEstadoCuenta = 1, IdEstadoAprobacion = rol == "Psicologo" ? 1 : null };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(rol);
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        if (rol == "Psicologo")
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
        SetupSesion(1, "Paciente");
        _catalogs.Setup(c => c.GetRolIdAsync("Psicologo", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Activa", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Aprobado", It.IsAny<CancellationToken>())).ReturnsAsync(5);
        _catalogs.Setup(c => c.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(8);
        _users.Setup(u => u.GetDirectorioPsicologosAsync(2, 1, 5, 8, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<PsicologoDirectorioRow> { new(1, "Laura Gomez", new List<string> { "Ansiedad" }, 2) });
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

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
        SetupSesion(5, "Psicologo");
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
}
