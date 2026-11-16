using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class CuestionarioServiceMoreTests
{
    private readonly Mock<ICuestionarioRepository> _cuestionarios = new();
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IUserRepository> _usuarios = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private CuestionarioService CreateSut() => new(
        _cuestionarios.Object, _historial.Object, _sesiones.Object, _usuarios.Object, _authLogs.Object);

    private void SetupSesion(int userId = 1)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 1, IdEstadoCuenta = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
    }

    [Fact]
    public async Task Finalizar_HappyPath_RetornaResultadoYLoguea()
    {
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "escala" } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 4 } }));
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new FinalizarCuestionarioVm { CuestionarioId = 1, Respuestas = new List<RespuestaCuestionarioVm> { new() { PreguntaId = 1, OpcionId = 1 } } };
        var result = await sut.FinalizarAsync(null, vm, "127.0.0.1", CancellationToken.None);

        result.CuestionarioId.Should().Be(1);
        result.Puntaje.Should().Be(4);
        result.Nivel.Should().Be("Alto");
        result.TotalPreguntas.Should().Be(1);
        result.Respondidas.Should().Be(1);
        _authLogs.Verify(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), LogAcciones.CuestionarioInvitadoFin, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
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
    public async Task ObtenerPendientePaciente_SinPendientes_RetornaNoDisponible()
    {
        SetupSesion();
        var publicados = new List<CuestionarioRow> { new() { IdCuestionario = 1, Titulo = "C1", Tipo = "Predeterminado", Estado = "Publicado" } };
        _cuestionarios.Setup(c => c.GetPublicadosInvitadoAsync(It.IsAny<CancellationToken>())).ReturnsAsync(publicados);
        _historial.Setup(h => h.GetCuestionariosRespondidosAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<int> { 1 });

        var sut = CreateSut();
        var result = await sut.ObtenerPendientePacienteAsync("token", CancellationToken.None);

        result.Disponible.Should().BeFalse();
        result.Mensaje.Should().Contain("No tienes cuestionarios pendientes");
    }

    [Fact]
    public async Task ObtenerPendientePaciente_ConPendiente_RetornaCuestionario()
    {
        SetupSesion();
        var publicados = new List<CuestionarioRow> { new() { IdCuestionario = 1, Titulo = "C1", Tipo = "Predeterminado", Estado = "Publicado", Descripcion = "Desc" } };
        _cuestionarios.Setup(c => c.GetPublicadosInvitadoAsync(It.IsAny<CancellationToken>())).ReturnsAsync(publicados);
        _historial.Setup(h => h.GetCuestionariosRespondidosAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<int>());
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "escala", Orden = 1 } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 3, Orden = 1 } }));

        var sut = CreateSut();
        var result = await sut.ObtenerPendientePacienteAsync("token", CancellationToken.None);

        result.Disponible.Should().BeTrue();
        result.Id.Should().Be(1);
        result.Preguntas.Should().HaveCount(1);
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

    [Fact]
    public async Task RegistrarInicioPaciente_Disponible_RegistraLog()
    {
        SetupSesion();
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _authLogs.Setup(a => a.LogAsync(1, 1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarInicioPacienteAsync("token", 1, "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(1, 1, LogAcciones.CuestionarioPacienteInicio, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FinalizarPaciente_HappyPath_RetornaResultadoYMarcaAsignacion()
    {
        SetupSesion();
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.FindAsignacionPendienteAsync(1, 1, It.IsAny<CancellationToken>())).Returns(Task.FromResult<AsignacionCuestionarioDto?>(null));
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "escala" } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 4 } }));
        _cuestionarios.Setup(c => c.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _historial.Setup(h => h.GetNivelIdAsync("Alto", It.IsAny<CancellationToken>())).ReturnsAsync(3);
        _historial.Setup(h => h.GuardarEventoCuestionarioAsync(1, 1, 3, 4, It.IsAny<List<RespuestaRow>>(), It.IsAny<CancellationToken>())).ReturnsAsync(99);
        _authLogs.Setup(a => a.LogAsync(1, 1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new FinalizarCuestionarioPacienteVm { CuestionarioId = 1, Respuestas = new List<RespuestaCuestionarioVm> { new() { PreguntaId = 1, OpcionId = 1 } } };
        var result = await sut.FinalizarPacienteAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.IdEvento.Should().Be(99);
        result.CuestionarioId.Should().Be(1);
        result.Puntaje.Should().Be(4);
        result.Nivel.Should().Be("Alto");
    }

    [Fact]
    public async Task FinalizarPaciente_NivelNoConfigurado_LanzaInvalidOperation()
    {
        SetupSesion();
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.FindAsignacionPendienteAsync(1, 1, It.IsAny<CancellationToken>())).Returns(Task.FromResult<AsignacionCuestionarioDto?>(null));
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "escala" } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 4 } }));
        _cuestionarios.Setup(c => c.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _historial.Setup(h => h.GetNivelIdAsync("Alto", It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);

        var sut = CreateSut();
        var vm = new FinalizarCuestionarioPacienteVm { CuestionarioId = 1, Respuestas = new List<RespuestaCuestionarioVm> { new() { PreguntaId = 1, OpcionId = 1 } } };
        var act = () => sut.FinalizarPacienteAsync("token", vm, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Nivel de riesgo no configurado*");
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
    public async Task ObtenerAsignadosPaciente_HappyPath_RetornaLista()
    {
        SetupSesion();
        var asign = new AsignacionCuestionarioDto(1, 2, "Titulo", "Personalizado", 1, 5, DateTime.UtcNow, null, "Pendiente");
        _cuestionarios.Setup(c => c.GetAsignadosPorPacienteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AsignacionCuestionarioDto> { asign });
        _authLogs.Setup(a => a.LogAsync(1, 1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerAsignadosPacienteAsync("token", "127.0.0.1", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].IdAsignacion.Should().Be(1);
        result[0].Titulo.Should().Be("Titulo");
    }

    [Fact]
    public async Task ObtenerAsignadoDetalle_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.ObtenerAsignadoDetalleAsync(null, 1, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ObtenerAsignadoDetalle_NoEncontrado_LanzaInvalidOperation()
    {
        SetupSesion();
        _cuestionarios.Setup(c => c.GetAsignadosPorPacienteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AsignacionCuestionarioDto>());

        var sut = CreateSut();
        var act = () => sut.ObtenerAsignadoDetalleAsync("token", 99, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Asignaci*n no encontrada*");
    }

    [Fact]
    public async Task ObtenerAsignadoDetalle_HappyPath_RetornaCuestionario()
    {
        SetupSesion();
        var asign = new AsignacionCuestionarioDto(1, 2, "Titulo", "Personalizado", 1, 5, DateTime.UtcNow, null, "Pendiente");
        _cuestionarios.Setup(c => c.GetAsignadosPorPacienteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<AsignacionCuestionarioDto> { asign });
        _cuestionarios.Setup(c => c.GetCompletoAsync(2, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 2, Texto = "P1", Tipo = "escala", Orden = 1 } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 3, Orden = 1 } }));
        _authLogs.Setup(a => a.LogAsync(1, 1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var result = await sut.ObtenerAsignadoDetalleAsync("token", 1, "127.0.0.1", CancellationToken.None);

        result.Disponible.Should().BeTrue();
        result.Id.Should().Be(2);
        result.Preguntas.Should().HaveCount(1);
    }

    [Fact]
    public async Task FinalizarPaciente_ConAsignacion_MarcaCompletada()
    {
        SetupSesion();
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado", Estado = "Publicado" };
        var asign = new AsignacionCuestionarioDto(5, 1, "Test", "Predeterminado", 1, 5, DateTime.UtcNow, null, "Pendiente");
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.FindAsignacionPendienteAsync(1, 1, It.IsAny<CancellationToken>())).ReturnsAsync(asign);
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "escala" } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 3 } }));
        _cuestionarios.Setup(c => c.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.MarcarAsignacionCompletadaAsync(5, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _historial.Setup(h => h.GetNivelIdAsync("Medio", It.IsAny<CancellationToken>())).ReturnsAsync(2);
        _historial.Setup(h => h.GuardarEventoCuestionarioAsync(1, 1, 2, 3, It.IsAny<List<RespuestaRow>>(), It.IsAny<CancellationToken>())).ReturnsAsync(77);
        _authLogs.Setup(a => a.LogAsync(1, 1, It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new FinalizarCuestionarioPacienteVm { CuestionarioId = 1, Respuestas = new List<RespuestaCuestionarioVm> { new() { PreguntaId = 1, OpcionId = 1 } } };
        var result = await sut.FinalizarPacienteAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.IdEvento.Should().Be(77);
        _cuestionarios.Verify(c => c.MarcarAsignacionCompletadaAsync(5, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task FinalizarPaciente_SinDisponibilidadNiAsignacion_LanzaInvalidOperation()
    {
        SetupSesion();
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((CuestionarioRow?)null);
        _cuestionarios.Setup(c => c.FindAsignacionPendienteAsync(1, 99, It.IsAny<CancellationToken>())).Returns(Task.FromResult<AsignacionCuestionarioDto?>(null));

        var sut = CreateSut();
        var vm = new FinalizarCuestionarioPacienteVm { CuestionarioId = 99, Respuestas = new List<RespuestaCuestionarioVm> { new() { PreguntaId = 1, OpcionId = 1 } } };
        var act = () => sut.FinalizarPacienteAsync("token", vm, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no disponible*");
    }
    [Fact]
    public async Task RegistrarInicio_Invitado_SesionLanza_UsaActorNull()
    {
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new CuestionarioRow { IdCuestionario = 1, Titulo = "T", Tipo = "Predeterminado", Estado = "Publicado" });
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("boom"));
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int?>(), It.IsAny<int?>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        await sut.RegistrarInicioAsync("token", 1, "127.0.0.1", CancellationToken.None);

        _authLogs.Verify(a => a.LogAsync(null, null, LogAcciones.CuestionarioInvitadoInicio, It.IsAny<string>(), "127.0.0.1", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegistrarInicio_Paciente_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.RegistrarInicioPacienteAsync(null, 1, "127.0.0.1", CancellationToken.None);
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}

