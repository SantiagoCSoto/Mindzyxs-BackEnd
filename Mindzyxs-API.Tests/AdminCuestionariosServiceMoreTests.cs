using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AdminCuestionariosServiceMoreTests
{
    private readonly Mock<ICuestionarioRepository> _cuestionarios = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly MindzyxsDbContext _db;

    public AdminCuestionariosServiceMoreTests()
    {
        var options = new Microsoft.EntityFrameworkCore.DbContextOptionsBuilder<MindzyxsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new MindzyxsDbContext(options);
    }

    private AdminCuestionariosService CreateSut() => new(
        _cuestionarios.Object, _users.Object, _sesiones.Object, _authLogs.Object, _db);

    private void SetupAdminSesion(int userId = 1)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 3, IdEstadoCuenta = 1 };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Administrador");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
    }

    [Fact]
    public async Task Listar_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.ListarAsync(null, null, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ObtenerDetalle_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.ObtenerDetalleAsync(null, 1, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Actualizar_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto { Titulo = "Test", Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 } } } } };
        var act = () => sut.ActualizarAsync(null, 1, dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Actualizar_NoEncontrado_LanzaInvalidOperation()
    {
        SetupAdminSesion();
        _cuestionarios.Setup(c => c.FindPredeterminadoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((CuestionarioRow?)null);

        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto { Titulo = "Test", Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 } } } } };
        var act = () => sut.ActualizarAsync("token", 99, dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task Actualizar_TituloVacio_LanzaInvalidOperation()
    {
        SetupAdminSesion();
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado" };
        _cuestionarios.Setup(c => c.FindPredeterminadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);

        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto { Titulo = "", Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 } } } } };
        var act = () => sut.ActualizarAsync("token", 1, dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*campos obligatorios*");
    }

    [Fact]
    public async Task Actualizar_SinPreguntas_LanzaInvalidOperation()
    {
        SetupAdminSesion();
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado" };
        _cuestionarios.Setup(c => c.FindPredeterminadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);

        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto { Titulo = "Test", Preguntas = new List<PreguntaCrearDto>() };
        var act = () => sut.ActualizarAsync("token", 1, dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*campos obligatorios*");
    }

    [Fact]
    public async Task Desactivar_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.DesactivarAsync(null, 1, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Desactivar_NoEncontrado_LanzaInvalidOperation()
    {
        SetupAdminSesion();
        _cuestionarios.Setup(c => c.FindPredeterminadoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((CuestionarioRow?)null);

        var sut = CreateSut();
        var act = () => sut.DesactivarAsync("token", 99, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }
}
