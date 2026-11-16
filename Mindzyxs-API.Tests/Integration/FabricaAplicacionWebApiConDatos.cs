using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mindzyxs_API;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;

namespace Mindzyxs_API.Tests.Integration;

public class FabricaAplicacionWebApiConDatos : WebApplicationFactory<Program>
{
    private static readonly object SeedLock = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(d => d.ServiceType == typeof(MindzyxsDbContext) || d.ServiceType == typeof(DbContextOptions<MindzyxsDbContext>)).ToList();
            foreach (var d in descriptors) services.Remove(d);

            services.AddDbContext<MindzyxsDbContext>(options =>
                options.UseInMemoryDatabase("SeededTestDb"));

            var authLogDescriptors = services.Where(d => d.ServiceType == typeof(IAuthLogRepository)).ToList();
            foreach (var d in authLogDescriptors) services.Remove(d);
            services.AddScoped<IAuthLogRepository, AuthLogRepositoryNoOp>();
        });
    }

    public void SeedDatabase()
    {
        lock (SeedLock)
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<MindzyxsDbContext>();
            if (db.Usuarios.Any()) return;

            db.Roles.AddRange(
                new RolRow { IdRol = 1, Nombre = "Paciente" },
                new RolRow { IdRol = 2, Nombre = "Psicologo" },
                new RolRow { IdRol = 3, Nombre = "Administrador" }
            );

            db.Estados.AddRange(
                new EstadoRow { IdEstado = 1, Nombre = "Activa" },
                new EstadoRow { IdEstado = 2, Nombre = "Suspendida" },
                new EstadoRow { IdEstado = 3, Nombre = "PendienteVerificacion" },
                new EstadoRow { IdEstado = 4, Nombre = "PendienteAprobacion" },
                new EstadoRow { IdEstado = 5, Nombre = "Aprobado" },
                new EstadoRow { IdEstado = 6, Nombre = "Rechazado" },
                new EstadoRow { IdEstado = 7, Nombre = "Inactiva" },
                new EstadoRow { IdEstado = 8, Nombre = "Autorizado" },
                new EstadoRow { IdEstado = 9, Nombre = "Revocado" }
            );

            db.Generos.AddRange(
                new GeneroRow { IdGenero = 1, Nombre = "Masculino" },
                new GeneroRow { IdGenero = 2, Nombre = "Femenino" }
            );

            db.TiposEvento.AddRange(
                new TipoEventoRow { IdTipoEvento = 1, Nombre = "RegistroEmocional" },
                new TipoEventoRow { IdTipoEvento = 2, Nombre = "Cuestionario" }
            );

            db.NivelesRiesgo.AddRange(
                new NivelRiesgoRow { IdNivelRiesgo = 1, Nombre = "Bajo" },
                new NivelRiesgoRow { IdNivelRiesgo = 2, Nombre = "Medio" },
                new NivelRiesgoRow { IdNivelRiesgo = 3, Nombre = "Alto" }
            );

            db.Especialidades.Add(new EspecialidadRow { IdEspecialidad = 1, Nombre = "Ansiedad" });

            db.Usuarios.AddRange(
                new UsuarioRow { IdUsuario = 1, Nombre = "Admin", Apellido = "Prueba", Correo = "admin@test.com", PasswordHash = "v1$210000$AAAAAAAAAAAAAAAAAAAAAA$BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB", FechaNacimiento = new DateTime(1990, 1, 1), IdRol = 3, IdEstadoCuenta = 1, IdGenero = 1, FechaRegistro = DateTime.UtcNow },
                new UsuarioRow { IdUsuario = 2, Nombre = "Camila", Apellido = "Rojas", Correo = "paciente@test.com", PasswordHash = "v1$210000$AAAAAAAAAAAAAAAAAAAAAA$BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB", FechaNacimiento = new DateTime(2002, 5, 10), IdRol = 1, IdEstadoCuenta = 1, IdGenero = 2, FechaRegistro = DateTime.UtcNow },
                new UsuarioRow { IdUsuario = 3, Nombre = "Laura", Apellido = "Gomez", Correo = "psicologo@test.com", PasswordHash = "v1$210000$AAAAAAAAAAAAAAAAAAAAAA$BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB", FechaNacimiento = new DateTime(1988, 7, 20), IdRol = 2, IdEstadoCuenta = 1, IdEstadoAprobacion = 5, IdGenero = 2, FechaRegistro = DateTime.UtcNow }
            );

            db.SaveChanges();
        }
    }
}
