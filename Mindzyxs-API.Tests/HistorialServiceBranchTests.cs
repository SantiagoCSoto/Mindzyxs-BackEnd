using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class HistorialServiceBranchTests
{
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<IUserRepository> _usuarios = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private HistorialService CreateSut() => new(
        _historial.Object, _usuarios.Object, _sesiones.Object, _authLogs.Object);

    private void SetupSesion(int userId = 1)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 1, IdEstadoCuenta = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
    }

    [Fact]
    public async Task Obtener_SinDesdeHasta_RetornaEventos()
    {
        SetupSesion();
        _historial.Setup(h => h.GetEventosAsync(1, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(
            new List<EventoRow> { new() { IdEvento = 1, IdPaciente = 1, IdTipoEvento = 1, FechaEvento = DateTime.UtcNow, PuntajeTotal = 3 } });
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string>());
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerAsync("token", null, null, "127.0.0.1", CancellationToken.None);

        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task Obtener_ConEventosCuestionario_ArmaItemConCuestionario()
    {
        SetupSesion();
        _historial.Setup(h => h.GetEventosAsync(1, null, null, It.IsAny<CancellationToken>())).ReturnsAsync(
            new List<EventoRow> { new() { IdEvento = 1, IdPaciente = 1, IdTipoEvento = 2, FechaEvento = DateTime.UtcNow, PuntajeTotal = 4, IdCuestionario = 5, IdNivelRiesgo = 2 } });
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string> { [2] = "Medio" });
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string> { [5] = "Cuest" });
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerAsync("token", null, null, "127.0.0.1", CancellationToken.None);

        result[0].Tipo.Should().Be("Cuestionario");
        result[0].Cuestionario.Should().Be("Cuest");
    }

    [Fact]
    public async Task ObtenerDetalle_ConRespuestas_ResuelveTextos()
    {
        SetupSesion();
        var ev = new EventoRow { IdEvento = 10, IdPaciente = 1, IdTipoEvento = 2, FechaEvento = DateTime.UtcNow, PuntajeTotal = 4, IdCuestionario = 5, IdNivelRiesgo = 2 };
        _historial.Setup(h => h.FindEventoAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string> { [2] = "Medio" });
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string> { [5] = "Cuest" });
        _historial.Setup(h => h.GetRespuestasAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<RespuestaRow> { new() { IdPregunta = 1, Valor = 3, Texto = null } });
        _historial.Setup(h => h.GetPreguntasTextoAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string> { [1] = "P1" });
        _historial.Setup(h => h.GetTextosOpcionAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<(int, int), string> { [(1, 3)] = "Opcion3" });
        _historial.Setup(h => h.GetObservacionesAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ObservacionRow>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerDetalleAsync("token", 10, "127.0.0.1", CancellationToken.None);

        result.Respuestas.Should().HaveCount(1);
        result.Respuestas[0].Texto.Should().Be("Opcion3");
    }

    [Fact]
    public async Task ObtenerDetalle_SinRespuestas_NoResuelve()
    {
        SetupSesion();
        var ev = new EventoRow { IdEvento = 10, IdPaciente = 1, IdTipoEvento = 1, FechaEvento = DateTime.UtcNow, PuntajeTotal = 4, IdNivelRiesgo = 2, Nota = "nota" };
        _historial.Setup(h => h.FindEventoAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(ev);
        _historial.Setup(h => h.GetNivelesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string> { [2] = "Medio" });
        _historial.Setup(h => h.GetCuestionariosTituloAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>())).ReturnsAsync(new Dictionary<int, string>());
        _historial.Setup(h => h.GetRespuestasAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<RespuestaRow>());
        _historial.Setup(h => h.GetObservacionesAsync(10, It.IsAny<CancellationToken>())).ReturnsAsync(new List<ObservacionRow>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerDetalleAsync("token", 10, "127.0.0.1", CancellationToken.None);

        result.Respuestas.Should().BeEmpty();
        result.Nota.Should().Be("nota");
    }

    [Fact]
    public async Task RegistrarEstadoEmocional_TipoNoConfigurado_Lanza()
    {
        SetupSesion();
        _historial.Setup(h => h.GetNivelIdAsync("Medio", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _historial.Setup(h => h.GetTipoEventoIdAsync("RegistroEmocional", It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);

        var sut = CreateSut();
        var vm = new RegistrarEstadoEmocionalVm { Puntaje = 3 };
        var act = () => sut.RegistrarEstadoEmocionalAsync("token", vm, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Tipo de evento*");
    }

    [Fact]
    public async Task RegistrarEstadoEmocional_NotaLarga_Lanza()
    {
        SetupSesion();
        _historial.Setup(h => h.GetNivelIdAsync("Medio", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _historial.Setup(h => h.GetTipoEventoIdAsync("RegistroEmocional", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = CreateSut();
        var vm = new RegistrarEstadoEmocionalVm { Puntaje = 3, Nota = new string('a', 501) };
        var act = () => sut.RegistrarEstadoEmocionalAsync("token", vm, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*500 caracteres*");
    }

    [Fact]
    public async Task RegistrarEstadoEmocional_ConNota_RetornaConNota()
    {
        SetupSesion();
        _historial.Setup(h => h.GetNivelIdAsync("Alto", It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _historial.Setup(h => h.GetTipoEventoIdAsync("RegistroEmocional", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _historial.Setup(h => h.GuardarEstadoEmocionalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(99);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new RegistrarEstadoEmocionalVm { Puntaje = 5, Nota = "  mi nota  " };
        var result = await sut.RegistrarEstadoEmocionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.IdEvento.Should().Be(99);
        result.Nota.Should().Be("mi nota");
    }

    [Fact]
    public async Task RegistrarEstadoEmocional_SinNota_RetornaNullNota()
    {
        SetupSesion();
        _historial.Setup(h => h.GetNivelIdAsync("Bajo", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _historial.Setup(h => h.GetTipoEventoIdAsync("RegistroEmocional", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _historial.Setup(h => h.GuardarEstadoEmocionalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), null, It.IsAny<DateTime>(), It.IsAny<CancellationToken>())).ReturnsAsync(100);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new RegistrarEstadoEmocionalVm { Puntaje = 1 };
        var result = await sut.RegistrarEstadoEmocionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.IdEvento.Should().Be(100);
        result.Nota.Should().BeNull();
    }
}
