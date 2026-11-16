using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AdminCuestionariosServiceHappyTests
{
    private readonly Mock<ICuestionarioRepository> _cuestionarios = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly MindzyxsDbContext _db;

    public AdminCuestionariosServiceHappyTests()
    {
        var options = new DbContextOptionsBuilder<MindzyxsDbContext>()
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
    public async Task Listar_HappyPath_RetornaLista()
    {
        SetupAdminSesion();
        _cuestionarios.Setup(c => c.GetPredeterminadosAsync(It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CuestionarioRow> { new() { IdCuestionario = 1, Titulo = "C1", Tipo = "Predeterminado", Estado = "Publicado", FechaCreacion = DateTime.UtcNow } });

        var sut = CreateSut();
        var result = await sut.ListarAsync("token", null, CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Titulo.Should().Be("C1");
    }

    [Fact]
    public async Task ObtenerDetalle_HappyPath_RetornaDetalle()
    {
        SetupAdminSesion();
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "C1", Tipo = "Predeterminado", Estado = "Publicado", FechaCreacion = DateTime.UtcNow };
        _cuestionarios.Setup(c => c.FindPredeterminadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "escala", Orden = 1 } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 3, Orden = 1 } }));

        var sut = CreateSut();
        var result = await sut.ObtenerDetalleAsync("token", 1, CancellationToken.None);

        result.Id.Should().Be(1);
        result.Preguntas.Should().HaveCount(1);
    }

    [Fact]
    public async Task Crear_HappyPath_RetornaId()
    {
        SetupAdminSesion();
        _cuestionarios.Setup(c => c.CrearPredeterminadoAsync(It.IsAny<CuestionarioRow>(), It.IsAny<List<(PreguntaRow, List<OpcionPreguntaRow>)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto
        {
            Titulo = "Test",
            Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 2 } } } }
        };
        var result = await sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        result.Id.Should().Be(42);
    }

    [Fact]
    public async Task Actualizar_HappyPath_RetornaId()
    {
        SetupAdminSesion();
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "C1", Tipo = "Predeterminado", Estado = "Borrador" };
        _cuestionarios.Setup(c => c.FindPredeterminadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.ActualizarPredeterminadoAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<List<(PreguntaRow, List<OpcionPreguntaRow>)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto
        {
            Titulo = "Updated",
            Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 2 } } } }
        };
        var result = await sut.ActualizarAsync("token", 1, dto, "127.0.0.1", CancellationToken.None);

        result.Id.Should().Be(1);
    }

    [Fact]
    public async Task Desactivar_HappyPath_RetornaMensaje()
    {
        SetupAdminSesion();
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "C1", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.FindPredeterminadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.DesactivarAsync(1, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.DesactivarAsync("token", 1, "127.0.0.1", CancellationToken.None);

        result.Should().Contain("desactivado");
    }
}
