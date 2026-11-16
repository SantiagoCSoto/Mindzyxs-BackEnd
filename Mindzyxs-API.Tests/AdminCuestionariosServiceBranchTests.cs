using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class AdminCuestionariosServiceBranchTests
{
    private readonly Mock<ICuestionarioRepository> _cuestionarios = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly MindzyxsDbContext _db;

    public AdminCuestionariosServiceBranchTests()
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
    public async Task Listar_SinBusqueda_RetornaLista()
    {
        SetupAdminSesion();
        _cuestionarios.Setup(c => c.GetPredeterminadosAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CuestionarioRow> { new() { IdCuestionario = 1, Titulo = "C1", Tipo = "Predeterminado", Estado = "Publicado", FechaCreacion = DateTime.UtcNow } });

        var sut = CreateSut();
        var result = await sut.ListarAsync("token", "   ", CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Listar_ConBusqueda_Filtra()
    {
        SetupAdminSesion();
        _cuestionarios.Setup(c => c.GetPredeterminadosAsync("test", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CuestionarioRow>());

        var sut = CreateSut();
        var result = await sut.ListarAsync("token", "test", CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Crear_SinTitulo_Lanza()
    {
        SetupAdminSesion();
        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto { Titulo = "  ", Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 2 } } } } };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Crear_SinPreguntas_Lanza()
    {
        SetupAdminSesion();
        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto { Titulo = "T", Preguntas = new List<PreguntaCrearDto>() };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Crear_PublicarTrue_EstadoPublicado()
    {
        SetupAdminSesion();
        _cuestionarios.Setup(c => c.CrearPredeterminadoAsync(It.IsAny<CuestionarioRow>(), It.IsAny<List<(PreguntaRow, List<OpcionPreguntaRow>)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto { Titulo = "T", Publicar = true, Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 2 } } } } };
        var result = await sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        result.Id.Should().Be(1);
    }

    [Fact]
    public async Task Crear_ConUmbralesValidos_ModoSuma()
    {
        SetupAdminSesion();
        _cuestionarios.Setup(c => c.CrearPredeterminadoAsync(It.IsAny<CuestionarioRow>(), It.IsAny<List<(PreguntaRow, List<OpcionPreguntaRow>)>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto
        {
            Titulo = "T",
            Umbrales = new UmbralesRiesgoDto { BajoMax = 1, MedioMax = 2 },
            Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 3 } } } }
        };
        var result = await sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        result.Id.Should().Be(2);
    }

    [Fact]
    public async Task Crear_UmbralesInvalidos_Lanza()
    {
        SetupAdminSesion();
        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto
        {
            Titulo = "T",
            Umbrales = new UmbralesRiesgoDto { BajoMax = 5, MedioMax = 3 },
            Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 3 } } } }
        };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ObtenerDetalle_NoEncontrado_Lanza()
    {
        SetupAdminSesion();
        _cuestionarios.Setup(c => c.FindPredeterminadoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((CuestionarioRow?)null);

        var sut = CreateSut();
        var act = () => sut.ObtenerDetalleAsync("token", 99, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task Actualizar_NoEncontrado_Lanza()
    {
        SetupAdminSesion();
        _cuestionarios.Setup(c => c.FindPredeterminadoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((CuestionarioRow?)null);

        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto { Titulo = "T", Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 2 } } } } };
        var act = () => sut.ActualizarAsync("token", 99, dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task Actualizar_TituloVacio_Lanza()
    {
        SetupAdminSesion();
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "C1", Tipo = "Predeterminado", Estado = "Borrador" };
        _cuestionarios.Setup(c => c.FindPredeterminadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);

        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto { Titulo = "", Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 2 } } } } };
        var act = () => sut.ActualizarAsync("token", 1, dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task Desactivar_NoEncontrado_Lanza()
    {
        SetupAdminSesion();
        _cuestionarios.Setup(c => c.FindPredeterminadoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((CuestionarioRow?)null);

        var sut = CreateSut();
        var act = () => sut.DesactivarAsync("token", 99, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task RequireAdmin_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto { Titulo = "T", Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 2 } } } } };
        var act = () => sut.CrearAsync(null, dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RequireAdmin_RolNoAdmin_LanzaInvalidOperation()
    {
        var sesion = new SesionDto(1, 1, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = 1, IdRol = 1, IdEstadoCuenta = 1 };
        _users.Setup(u => u.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");

        var sut = CreateSut();
        var dto = new CuestionarioPredeterminadoCrearDto { Titulo = "T", Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 }, new() { Texto = "O2", Valor = 2 } } } } };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*permisos*");
    }
}
