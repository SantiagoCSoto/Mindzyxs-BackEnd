using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class RecordatorioServiceMoreTests
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

    [Fact]
    public async Task Obtener_ConConfiguracionDiaria_RetornaDto()
    {
        SetupSesion();
        var row = new RecordatorioRow { IdRecordatorio = 1, IdPaciente = 1, Titulo = "Test", Hora = new TimeSpan(8, 0, 0), Activo = true, Frecuencia = "Diaria", Mensaje = "Msg" };
        _recordatorios.Setup(r => r.GetDePacienteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(row);

        var sut = CreateSut();
        var result = await sut.ObtenerAsync("token", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Frecuencia.Should().Be("Diaria");
        result.Hora.Should().Be("08:00");
        result.Dias.Should().BeEmpty();
    }

    [Fact]
    public async Task Obtener_ConConfiguracionSemanal_RetornaDias()
    {
        SetupSesion();
        var row = new RecordatorioRow { IdRecordatorio = 1, IdPaciente = 1, Titulo = "Test", Hora = new TimeSpan(9, 30, 0), Activo = true, Frecuencia = "Semanal", Mensaje = "Msg" };
        _recordatorios.Setup(r => r.GetDePacienteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(row);
        _recordatorios.Setup(r => r.GetDiasAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(new List<int> { 1, 3, 5 });

        var sut = CreateSut();
        var result = await sut.ObtenerAsync("token", CancellationToken.None);

        result.Should().NotBeNull();
        result!.Frecuencia.Should().Be("Semanal");
        result.Dias.Should().HaveCount(3);
    }

    [Fact]
    public async Task Hoy_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var act = () => sut.HoyAsync(null, CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Hoy_ConAvisos_RetornaLista()
    {
        SetupSesion();
        var row = new RecordatorioRow { IdRecordatorio = 1, IdPaciente = 1, Titulo = "Test", Hora = new TimeSpan(7, 0, 0), Activo = true, Frecuencia = "Diaria", Mensaje = "Msg" };
        _recordatorios.Setup(r => r.GetDePacienteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync(row);

        var sut = CreateSut();
        var result = await sut.HoyAsync("token", CancellationToken.None);

        result.Should().HaveCount(1);
        result[0].Titulo.Should().Be("Test");
    }

    [Fact]
    public async Task Hoy_SinAvisos_RetornaListaVacia()
    {
        SetupSesion();
        _recordatorios.Setup(r => r.GetDePacienteAsync(1, It.IsAny<CancellationToken>())).ReturnsAsync((RecordatorioRow?)null);

        var sut = CreateSut();
        var result = await sut.HoyAsync("token", CancellationToken.None);

        result.Should().BeEmpty();
    }
}
