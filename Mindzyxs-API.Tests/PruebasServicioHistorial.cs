using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioHistorial
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
        _usuarios.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _usuarios.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
    }

    [Fact]
    public async Task RegistrarEstadoEmocional_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var vm = new RegistrarEstadoEmocionalVm { Puntaje = 3 };
        var act = () => sut.RegistrarEstadoEmocionalAsync(null, vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(-1)]
    public async Task RegistrarEstadoEmocional_PuntajeInvalido_LanzaInvalidOperation(int? puntaje)
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new RegistrarEstadoEmocionalVm { Puntaje = puntaje };
        var act = () => sut.RegistrarEstadoEmocionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*estado emocional*");
    }

    [Fact]
    public async Task RegistrarEstadoEmocional_NotaMuyLarga_LanzaInvalidOperation()
    {
        SetupSesion();
        _historial.Setup(h => h.GetNivelIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _historial.Setup(h => h.GetTipoEventoIdAsync("RegistroEmocional", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var sut = CreateSut();
        var vm = new RegistrarEstadoEmocionalVm { Puntaje = 3, Nota = new string('a', 501) };
        var act = () => sut.RegistrarEstadoEmocionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*500 caracteres*");
    }

    [Fact]
    public async Task RegistrarEstadoEmocional_NivelNoConfigurado_LanzaInvalidOperation()
    {
        SetupSesion();
        _historial.Setup(h => h.GetNivelIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);
        var sut = CreateSut();
        var vm = new RegistrarEstadoEmocionalVm { Puntaje = 3 };
        var act = () => sut.RegistrarEstadoEmocionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Nivel de riesgo no configurado*");
    }

    [Fact]
    public async Task RegistrarEstadoEmocional_TipoEventoNoConfigurado_LanzaInvalidOperation()
    {
        SetupSesion();
        _historial.Setup(h => h.GetNivelIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _historial.Setup(h => h.GetTipoEventoIdAsync("RegistroEmocional", It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);
        var sut = CreateSut();
        var vm = new RegistrarEstadoEmocionalVm { Puntaje = 3 };
        var act = () => sut.RegistrarEstadoEmocionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*Tipo de evento RegistroEmocional no configurado*");
    }

    [Fact]
    public async Task RegistrarEstadoEmocional_Valido_GuardaYRetornaResultado()
    {
        SetupSesion();
        _historial.Setup(h => h.GetNivelIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _historial.Setup(h => h.GetTipoEventoIdAsync("RegistroEmocional", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _historial.Setup(h => h.GuardarEstadoEmocionalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(42);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new RegistrarEstadoEmocionalVm { Puntaje = 4, Nota = "Me sentí bien" };
        var result = await sut.RegistrarEstadoEmocionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.IdEvento.Should().Be(42);
        result.Puntaje.Should().Be(4);
        result.Nivel.Should().Be("Alto");
        result.Nota.Should().Be("Me sentí bien");
    }

    [Fact]
    public async Task RegistrarEstadoEmocional_SinNota_GuardaConNotaNull()
    {
        SetupSesion();
        _historial.Setup(h => h.GetNivelIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _historial.Setup(h => h.GetTipoEventoIdAsync("RegistroEmocional", It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _historial.Setup(h => h.GuardarEstadoEmocionalAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string?>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(43);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new RegistrarEstadoEmocionalVm { Puntaje = 3, Nota = "   " };
        var result = await sut.RegistrarEstadoEmocionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.Nota.Should().BeNull();
    }
}

