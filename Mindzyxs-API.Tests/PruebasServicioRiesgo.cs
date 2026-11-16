using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioRiesgo
{
    private readonly Mock<ICuestionarioRepository> _cuestionarios = new();
    private readonly Mock<ILineaAyudaRepository> _lineas = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<IUserRepository> _usuarios = new();

    private RiesgoService CreateSut() => new(
        _cuestionarios.Object, _lineas.Object, _sesiones.Object, _authLogs.Object, _usuarios.Object);

    private void SetupSesion(int userId = 1)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 1, IdEstadoCuenta = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _usuarios.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _usuarios.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
    }

    [Fact]
    public async Task RegistrarAlerta_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var vm = new RegistrarAlertaRiesgoVm { Accion = "vista" };
        var act = () => sut.RegistrarAlertaAsync(null, vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task RegistrarAlerta_AccionVacia_LanzaInvalidOperation(string? accion)
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new RegistrarAlertaRiesgoVm { Accion = accion ?? string.Empty };
        var act = () => sut.RegistrarAlertaAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*opci* de la alerta*");
    }

    [Theory]
    [InlineData("invalida")]
    [InlineData("cerrar")]
    [InlineData("ok")]
    public async Task RegistrarAlerta_AccionInvalida_LanzaInvalidOperation(string accion)
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new RegistrarAlertaRiesgoVm { Accion = accion ?? string.Empty };
        var act = () => sut.RegistrarAlertaAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Acci*n de la alerta no v*");
    }

    [Theory]
    [InlineData("vista")]
    [InlineData("lineas")]
    [InlineData("recomendaciones")]
    [InlineData("entendido")]
    public async Task RegistrarAlerta_AccionValida_RetornaRegistrada(string accion)
    {
        SetupSesion();
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new RegistrarAlertaRiesgoVm { Accion = accion, Origen = "cuestionario" };
        var result = await sut.RegistrarAlertaAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.Should().Be("Alerta registrada");
    }

    [Fact]
    public async Task RegistrarAlerta_AccionVista_RegistraVista()
    {
        SetupSesion();
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), LogAcciones.AlertaRiesgoVista, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new RegistrarAlertaRiesgoVm { Accion = "vista", Origen = "cuestionario" };
        await sut.RegistrarAlertaAsync("token", vm, "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), LogAcciones.AlertaRiesgoVista, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarAlerta_AccionLineas_RegistraAccion()
    {
        SetupSesion();
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), LogAcciones.AlertaRiesgoAccion, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new RegistrarAlertaRiesgoVm { Accion = "lineas", Origen = "cuestionario" };
        await sut.RegistrarAlertaAsync("token", vm, "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), LogAcciones.AlertaRiesgoAccion, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Calcular_CuestionarioNoDisponible_LanzaInvalidOperation()
    {
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(99, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CuestionarioRow?)null);

        var sut = CreateSut();
        var vm = new CalcularRiesgoVm { CuestionarioId = 99, Respuestas = new List<RespuestaCuestionarioVm> { new() { PreguntaId = 1, OpcionId = 1 } } };
        var act = () => sut.CalcularAsync(null, vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no disponible*");
    }

    [Fact]
    public async Task Calcular_SinRespuestasCompletas_LanzaInvalidOperation()
    {
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "escala" } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 3 } }));

        var sut = CreateSut();
        var vm = new CalcularRiesgoVm { CuestionarioId = 1, Respuestas = new List<RespuestaCuestionarioVm>() };
        var act = () => sut.CalcularAsync(null, vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*No se encontraron respuestas*");
    }

    [Fact]
    public async Task Calcular_OpcionInvalida_LanzaInvalidOperation()
    {
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "escala" } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 3 } }));

        var sut = CreateSut();
        var vm = new CalcularRiesgoVm { CuestionarioId = 1, Respuestas = new List<RespuestaCuestionarioVm> { new() { PreguntaId = 1, OpcionId = 99 } } };
        var act = () => sut.CalcularAsync(null, vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no es válida*");
    }

    [Fact]
    public async Task Calcular_HappyPath_RetornaDtoYLoguea()
    {
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "escala" } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 5 } }));
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new CalcularRiesgoVm { CuestionarioId = 1, Respuestas = new List<RespuestaCuestionarioVm> { new() { PreguntaId = 1, OpcionId = 1 } } };
        var result = await sut.CalcularAsync(null, vm, "127.0.0.1", CancellationToken.None);

        result.Nivel.Should().Be("Alto");
        result.Puntaje.Should().Be(5);
        result.Alerta.Mostrar.Should().BeTrue();
        result.MostrarLineasAyuda.Should().BeTrue();
        _authLogs.Verify(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), LogAcciones.RiesgoInvitadoCalculo, It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Calcular_ConSesion_RegistraActorUsuario()
    {
        var sesion = new SesionDto(1, 7, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "binaria" } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 1 } }));
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new CalcularRiesgoVm { CuestionarioId = 1, Respuestas = new List<RespuestaCuestionarioVm> { new() { PreguntaId = 1, OpcionId = 1 } } };
        var result = await sut.CalcularAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.Nivel.Should().Be("Bajo");
        result.Puntaje.Should().Be(0);
    }

    [Fact]
    public async Task ObtenerLineas_ConLineas_RetornaMapeadas()
    {
        _lineas.Setup(l => l.GetActivasAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<LineaAyudaRow>
        {
            new() { IdLinea = 1, Nombre = "L1", Telefono = "123", Fuente = "src" }
        });

        var sut = CreateSut();
        var result = await sut.ObtenerLineasAsync(CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Id.Should().Be(1);
        result[0].Nombre.Should().Be("L1");
        result[0].Telefono.Should().Be("123");
    }

    [Fact]
    public async Task ObtenerLineas_SinLineas_RetornaVacio()
    {
        _lineas.Setup(l => l.GetActivasAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<LineaAyudaRow>());

        var sut = CreateSut();
        var result = await sut.ObtenerLineasAsync(CancellationToken.None);

        result.Should().BeEmpty();
    }
}

