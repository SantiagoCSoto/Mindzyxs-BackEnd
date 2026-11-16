using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioRecordatorio
{
    private readonly Mock<IRecordatorioRepository> _recordatorios = new();
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();

    private RecordatorioService CreateSut() => new(
        _recordatorios.Object, _users.Object, _sesiones.Object, _authLogs.Object);

    private void SetupSesion(int userId = 1)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 1, IdEstadoCuenta = 1 };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Paciente");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
    }

    [Fact]
    public async Task Obtener_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.ObtenerAsync(null, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Obtener_SinConfiguracion_RetornaNull()
    {
        SetupSesion();
        _recordatorios.Setup(r => r.GetDePacienteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((RecordatorioRow?)null);

        var sut = CreateSut();
        var result = await sut.ObtenerAsync("token", CancellationToken.None);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Guardar_SinFrecuencia_LanzaInvalidOperation(string? frecuencia)
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new GuardarRecordatorioVm { Frecuencia = frecuencia, Hora = "08:00" };
        var act = () => sut.GuardarAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*frecuencia y hora*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("invalid")]
    public async Task Guardar_HoraInvalida_LanzaInvalidOperation(string? hora)
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new GuardarRecordatorioVm { Frecuencia = "Diaria", Hora = hora };
        var act = () => sut.GuardarAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*frecuencia y hora*");
    }

    [Fact]
    public async Task Guardar_FrecuenciaInvalida_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new GuardarRecordatorioVm { Frecuencia = "Mensual", Hora = "08:00" };
        var act = () => sut.GuardarAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*frecuencia y hora*");
    }

    [Fact]
    public async Task Guardar_SemanalSinDias_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new GuardarRecordatorioVm { Frecuencia = "Semanal", Hora = "08:00", Dias = new List<int>() };
        var act = () => sut.GuardarAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*frecuencia y hora*");
    }

    [Fact]
    public async Task Guardar_Valido_RetornaConfiguracion()
    {
        SetupSesion();
        var row = new RecordatorioRow { IdRecordatorio = 1, IdPaciente = 1, Titulo = "Registro emocional", Hora = new TimeSpan(8, 0, 0), Activo = true, Frecuencia = "Diaria", Mensaje = "Test" };
        _recordatorios.Setup(r => r.GuardarAsync(It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<TimeSpan>(), It.IsAny<string>(), It.IsAny<bool>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(row);
        _recordatorios.Setup(r => r.GetDiasAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<int>());
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new GuardarRecordatorioVm { Frecuencia = "Diaria", Hora = "08:00", Titulo = "Test", Activo = true };
        var result = await sut.GuardarAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.Frecuencia.Should().Be("Diaria");
        result.Hora.Should().Be("08:00");
        result.Activo.Should().BeTrue();
    }
}

