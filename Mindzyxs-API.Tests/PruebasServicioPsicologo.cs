using FluentAssertions;
using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using Moq;
using Xunit;

namespace Mindzyxs_API.Tests.Services;

public class PruebasServicioPsicologo
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<ICatalogRepository> _catalogs = new();
    private readonly Mock<ISesionRepository> _sesiones = new();
    private readonly Mock<IAuthLogRepository> _authLogs = new();
    private readonly Mock<ITarjetaStorage> _tarjetas = new();

    private PsicologoService CreateSut() => new(
        _users.Object, _catalogs.Object, _sesiones.Object, _authLogs.Object, _tarjetas.Object);

    private void SetupSesion(int userId = 5)
    {
        var sesion = new SesionDto(1, userId, "hash", DateTime.UtcNow.AddHours(1), DateTime.UtcNow);
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(sesion));
        var user = new UsuarioDto { IdUsuario = userId, IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 1, Correo = "psi@test.com" };
        _users.Setup(u => u.FindByIdAsync(userId, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _users.Setup(u => u.GetRolNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Psicologo");
        _users.Setup(u => u.GetEstadoCuentaNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Activa");
        _catalogs.Setup(c => c.GetEstadoNombreAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("Aprobado");
    }

    [Fact]
    public async Task ActualizarPerfil_SinSesion_LanzaUnauthorized()
    {
        _sesiones.Setup(s => s.FindByTokenHashAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult<SesionDto?>(null));

        var sut = CreateSut();
        var vm = new ActualizarPerfilProfesionalVm();
        var act = () => sut.ActualizarPerfilProfesionalAsync(null, vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task ActualizarPerfil_AniosNegativos_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new ActualizarPerfilProfesionalVm { AñosExperiencia = -1 };
        var act = () => sut.ActualizarPerfilProfesionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*negativos*");
    }

    [Fact]
    public async Task ActualizarPerfil_CorreoInvalido_LanzaInvalidOperation()
    {
        SetupSesion();
        var sut = CreateSut();
        var vm = new ActualizarPerfilProfesionalVm { Correo = "invalid" };
        var act = () => sut.ActualizarPerfilProfesionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*correo válido*");
    }

    [Fact]
    public async Task ActualizarPerfil_CorreoDuplicado_LanzaInvalidOperation()
    {
        SetupSesion();
        _users.Setup(u => u.ExistsByCorreoAsync("dup@test.com", It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var sut = CreateSut();
        var vm = new ActualizarPerfilProfesionalVm { Correo = "dup@test.com" };
        var act = () => sut.ActualizarPerfilProfesionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*ya se encuentra registrado*");
    }

    [Fact]
    public async Task ActualizarPerfil_EspecialidadInexistente_LanzaInvalidOperation()
    {
        SetupSesion();
        _catalogs.Setup(c => c.ExisteEspecialidadAsync(999, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var sut = CreateSut();
        var vm = new ActualizarPerfilProfesionalVm { Especialidades = new List<int> { 999 } };
        var act = () => sut.ActualizarPerfilProfesionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*no existe*");
    }

    [Fact]
    public async Task ActualizarPerfil_Valido_RetornaMensajeExito()
    {
        SetupSesion();
        _users.Setup(u => u.ActualizarPerfilProfesionalAsync(It.IsAny<int>(), It.IsAny<int?>(), It.IsAny<string?>(), It.IsAny<string?>(), It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _authLogs.Setup(a => a.LogAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var sut = CreateSut();
        var vm = new ActualizarPerfilProfesionalVm { AñosExperiencia = 10, DescripcionProfesional = "Psicólogo clínico" };
        var result = await sut.ActualizarPerfilProfesionalAsync("token", vm, "127.0.0.1", CancellationToken.None);

        result.Should().Contain("actualizada correctamente");
    }
}

