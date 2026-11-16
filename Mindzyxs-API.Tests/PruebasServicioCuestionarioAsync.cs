using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioCuestionarioAsync
{
    private readonly Mock<ICuestionarioRepository> _cuestionarios = new();
    private readonly Mock<IHistorialRepository> _historial = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IUserRepository> _usuarios = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private CuestionarioService CreateSut() => new(
        _cuestionarios.Object, _historial.Object, _sesiones.Object, _usuarios.Object, _authLogs.Object);

    [Fact]
    public async Task Finalizar_Invitado_CuestionarioNoDisponible_LanzaInvalidOperation()
    {
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((CuestionarioRow?)null);

        var sut = CreateSut();
        var vm = new FinalizarCuestionarioVm { CuestionarioId = 99, Respuestas = new List<RespuestaCuestionarioVm> { new() { PreguntaId = 1, OpcionId = 1 } } };
        var act = () => sut.FinalizarAsync(null, vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no disponible*");
    }

    [Fact]
    public async Task Finalizar_Invitado_SinRespuestas_LanzaInvalidOperation()
    {
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "escala" } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 3 } }));

        var sut = CreateSut();
        var vm = new FinalizarCuestionarioVm { CuestionarioId = 1, Respuestas = new List<RespuestaCuestionarioVm>() };
        var act = () => sut.FinalizarAsync(null, vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*seleccionar una respuesta*");
    }

    [Fact]
    public async Task Finalizar_Invitado_OpcionInvalida_LanzaInvalidOperation()
    {
        var cuest = new CuestionarioRow { IdCuestionario = 1, Titulo = "Test", Tipo = "Predeterminado", Estado = "Publicado" };
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(cuest);
        _cuestionarios.Setup(c => c.GetCompletoAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(
            (new List<PreguntaRow> { new() { IdPregunta = 1, IdCuestionario = 1, Texto = "P1", Tipo = "escala" } },
             new List<OpcionPreguntaRow> { new() { IdOpcion = 1, IdPregunta = 1, Texto = "O1", Valor = 3 } }));

        var sut = CreateSut();
        var vm = new FinalizarCuestionarioVm { CuestionarioId = 1, Respuestas = new List<RespuestaCuestionarioVm> { new() { PreguntaId = 1, OpcionId = 99 } } };
        var act = () => sut.FinalizarAsync(null, vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no es válida*");
    }

    [Fact]
    public async Task Finalizar_Paciente_CuestionarioNoDisponible_LanzaInvalidOperation()
    {
        var sesion = new SesionDto(1, 1, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = 1, IdRol = 1, IdEstadoCuenta = 1 };
        _usuarios.Setup(u => u.FindByIdAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _usuarios.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _usuarios.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _cuestionarios.Setup(c => c.FindPublicadoInvitadoAsync(99, It.IsAny<CancellationToken>())).ReturnsAsync((CuestionarioRow?)null);
        _cuestionarios.Setup(c => c.FindAsignacionPendienteAsync(1, 99, It.IsAny<CancellationToken>())).Returns(Task.FromResult<AsignacionCuestionarioDto?>(null));

        var sut = CreateSut();
        var vm = new FinalizarCuestionarioPacienteVm { CuestionarioId = 99, Respuestas = new List<RespuestaCuestionarioVm> { new() { PreguntaId = 1, OpcionId = 1 } } };
        var act = () => sut.FinalizarPacienteAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no disponible*");
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
}

