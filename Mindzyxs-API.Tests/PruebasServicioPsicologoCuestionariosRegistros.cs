using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PsicologoCuestionariosServiceTests
{
    private readonly Mock<ICuestionarioRepository> _cuestionarios = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<IHistorialRepository> _historial = new();

    private PsicologoCuestionariosService CreateSut() => new(
        _cuestionarios.Object, _users.Object, _catalogs.Object, _sesiones.Object, _authLogs.Object, _historial.Object);

    private void SetupSesion(int userId = 5)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1 };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");
    }

    [Fact]
    public async Task Crear_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var dto = new CuestionarioCrearDto { Titulo = "Test", Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 } } } } };
        var act = () => sut.CrearAsync(null, dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Crear_TituloVacio_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var dto = new CuestionarioCrearDto { Titulo = "", Preguntas = new List<PreguntaCrearDto> { new() { Texto = "P1", Opciones = new List<OpcionCrearDto> { new() { Texto = "O1", Valor = 1 } } } } };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*título es obligatorio*");
    }

    [Fact]
    public async Task Crear_SinPreguntas_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var dto = new CuestionarioCrearDto { Titulo = "Test", Preguntas = new List<PreguntaCrearDto>() };
        var act = () => sut.CrearAsync("token", dto, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*al menos una pregunta*");
    }

    [Fact]
    public async Task ObtenerDetalle_NoEncontrado_LanzaInvalidOperation()
    {
        SetupSesion();
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(99, 5, It.IsAny<CancellationToken>())).ReturnsAsync((CuestionarioRow?)null);

        var sut = CreateSut();
        var act = () => sut.ObtenerDetalleAsync("token", 99, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task Despublicar_NoEncontrado_LanzaInvalidOperation()
    {
        SetupSesion();
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(99, 5, It.IsAny<CancellationToken>())).ReturnsAsync((CuestionarioRow?)null);

        var sut = CreateSut();
        var act = () => sut.DespublicarAsync("token", 99, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }

    [Fact]
    public async Task Eliminar_SinConfirmacion_LanzaInvalidOperation()
    {
        SetupSesion();
        var cuest = new CuestionarioRow { IdCuestionario = 1, IdCreador = 5, Titulo = "Test", Tipo = "Personalizado", Estado = "Borrador" };
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(1, 5, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);

        var sut = CreateSut();
        var act = () => sut.EliminarAsync("token", 1, false, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*confirmar la eliminación*");
    }

    [Fact]
    public async Task Eliminar_NoEncontrado_LanzaInvalidOperation()
    {
        SetupSesion();
        _cuestionarios.Setup(c => c.FindDePsicologoAsync(99, 5, It.IsAny<CancellationToken>())).ReturnsAsync((CuestionarioRow?)null);

        var sut = CreateSut();
        var act = () => sut.EliminarAsync("token", 99, true, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no encontrado*");
    }
}

public class PsicologoRegistrosServiceTests
{
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private PsicologoRegistrosService CreateSut() => new(
        _historial.Object, _users.Object, _catalogs.Object, _sesiones.Object, _authLogs.Object);

    private void SetupSesion(int userId = 5)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1 };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");
    }

    [Fact]
    public async Task Registros_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.RegistrosAsync(null, 1, null, null, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Registros_PacienteNoAutorizado_LanzaInvalidOperation()
    {
        SetupSesion();
        var idAut = 8;
        _historial.Setup(h => h.GetEstadoIdAsync("Autorizado", It.IsAny<CancellationToken>())).ReturnsAsync(idAut);
        _historial.Setup(h => h.EstaAutorizadoAsync(1, 5, idAut, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        var act = () => sut.RegistrosAsync("token", 1, null, null, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*autorización*");
    }

    [Fact]
    public async Task Detalle_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.DetalleAsync(null, 1, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}

