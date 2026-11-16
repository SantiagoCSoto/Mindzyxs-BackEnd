using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioCuestionarioMas
{
    private readonly Mock<ICuestionarioRepository> _cuestionarios = new();
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IUserRepository> _usuarios = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private CuestionarioService CreateSut() => new(
        _cuestionarios.Object, _historial.Object, _sesiones.Object, _usuarios.Object, _authLogs.Object);

    [Fact]
    public async Task ObtenerPendiente_Invitado_SinCuestionarios_RetornaNull()
    {
        _cuestionarios.Setup(c => c.GetPublicadosInvitadoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CuestionarioRow>());

        var sut = CreateSut();
        var result = await sut.ObtenerPendienteAsync(new HashSet<int>());

        result.Should().NotBeNull();
        result!.Disponible.Should().BeFalse();
        result.Mensaje.Should().Contain("No tienes cuestionarios pendientes");
    }

    [Fact]
    public async Task ObtenerPendiente_Invitado_ConCuestionarios_RetornaCuestionario()
    {
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.GetPublicadosInvitadoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CuestionarioRow> { cuest });
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "escala" } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 3 } }));

        var sut = CreateSut();
        var result = await sut.ObtenerPendienteAsync(new HashSet<int>());

        result.Should().NotBeNull();
        result!.Id.Should().Be(1);
        result.Preguntas.Should().HaveCount(1);
    }

    [Fact]
    public async Task ObtenerPendiente_Invitado_ExcluyeRespondidos()
    {
        var c1 = new CuestionarioRow { IdCuestionario = 1, Titulo = "C1", Tipo = "Predeterminado", Estado = "Publicado" };
        var c2 = new CuestionarioRow { IdCuestionario = 2, Titulo = "C2", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.GetPublicadosInvitadoAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<CuestionarioRow> { c1, c2 });
        _cuestionarios.Setup(c => c.GetCompletoAsync(2, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<PreguntaRow>(), new List<OpcionPreguntaRow>()));

        var sut = CreateSut();
        var result = await sut.ObtenerPendienteAsync(new HashSet<int> { 1 });

        result.Should().NotBeNull();
        result!.Id.Should().Be(2);
    }

    [Fact]
    public async Task RegistrarInicio_Invitado_CuestionarioDisponible_Registra()
    {
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarInicioAsync(null, 1, "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), LogAcciones.CuestionarioInvitadoInicio, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarInicio_Invitado_CuestionarioNoDisponible_RegistraError()
    {
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((CuestionarioRow?)null);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarInicioAsync(null, 99, "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), LogAcciones.CuestionarioInvitadoInicio, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ObtenerPendientePaciente_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.ObtenerPendientePacienteAsync(null, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ObtenerAsignadosPaciente_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.ObtenerAsignadosPacienteAsync(null, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task RegistrarInicioPaciente_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.RegistrarInicioPacienteAsync(null, 1, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}

