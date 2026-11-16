using Microsoft.EntityFrameworkCore;
using Mindzyxs_API.Repositories.Models;

namespace Mindzyxs_API.Tests.Integration;

/// <summary>
/// Infraestructura de aislamiento para pruebas SQL. DryRun habilitado. Execute deshabilitado (confirm=false por defecto).
/// </summary>
public static class SqlTestCleanup
{
    public class TestScope : IDisposable
    {
        public Guid RunId { get; } = Guid.NewGuid();
        public List<int> UserIds { get; } = new();
        public List<int> SesionIds { get; } = new();
        public List<int> AutorizacionIds { get; } = new();
        public List<int> EventoIds { get; } = new();
        public List<int> CodigoIds { get; } = new();

        public void Dispose() { GC.SuppressFinalize(this); }
    }

    public static bool IsSafeContext(MindzyxsDbContext db)
    {
        try
        {
            var dbName = db.Database.SqlQueryRaw<string>("SELECT DB_NAME()").AsEnumerable().FirstOrDefault();
            return string.Equals(dbName, "Mindzyxs_Test", StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static DryRunResult DryRun(MindzyxsDbContext db, TestScope scope)
    {
        var result = new DryRunResult { RunId = scope.RunId };

        if (!IsSafeContext(db))
        {
            result.Safe = false;
            result.Reason = "Contexto no seguro: la base no es Mindzyxs_Test.";
            return result;
        }
        result.Safe = true;

        result.UserIds = scope.UserIds.ToList();
        result.SesionIds = scope.SesionIds.ToList();
        result.AutorizacionIds = scope.AutorizacionIds.ToList();
        result.EventoIds = scope.EventoIds.ToList();
        result.CodigoIds = scope.CodigoIds.ToList();

        result.Warnings.Add("La limpieza real no está habilitada en esta sesión.");
        result.Warnings.Add("Los IDs deben ser registrados explícitamente por cada prueba.");

        return result;
    }

    public static void Execute(MindzyxsDbContext db, TestScope scope, bool confirm = false)
    {
        if (!confirm)
            throw new InvalidOperationException("Limpieza no autorizada. Pase confirm=true para ejecutar.");

        if (!IsSafeContext(db))
            throw new InvalidOperationException("Contexto no seguro: la base no es Mindzyxs_Test.");

        if (scope.EventoIds.Count > 0)
        {
            foreach (var __id in scope.EventoIds)
            {
                db.Database.ExecuteSqlRaw("DELETE FROM dbo.respuestas WHERE id_evento = @p0", new Microsoft.Data.SqlClient.SqlParameter("@p0", __id));
                db.Database.ExecuteSqlRaw("DELETE FROM dbo.eventos_bienestar WHERE id_evento = @p0", new Microsoft.Data.SqlClient.SqlParameter("@p0", __id));
            }
        }
        if (scope.AutorizacionIds.Count > 0)
        {
            foreach (var __id in scope.AutorizacionIds)
                db.Database.ExecuteSqlRaw("DELETE FROM dbo.autorizaciones WHERE id_autorizacion = @p0", new Microsoft.Data.SqlClient.SqlParameter("@p0", __id));
        }
        if (scope.SesionIds.Count > 0)
        {
            foreach (var __id in scope.SesionIds)
                db.Database.ExecuteSqlRaw("DELETE FROM dbo.sesiones WHERE id_sesion = @p0", new Microsoft.Data.SqlClient.SqlParameter("@p0", __id));
        }
        if (scope.CodigoIds.Count > 0)
        {
            foreach (var __id in scope.CodigoIds)
                db.Database.ExecuteSqlRaw("DELETE FROM dbo.codigos_verificacion WHERE id_codigo = @p0", new Microsoft.Data.SqlClient.SqlParameter("@p0", __id));
        }
        if (scope.UserIds.Count > 0)
        {
            foreach (var __id in scope.UserIds)
                db.Database.ExecuteSqlRaw("DELETE FROM dbo.usuarios WHERE id_usuario = @p0", new Microsoft.Data.SqlClient.SqlParameter("@p0", __id));
        }
    }

    public class DryRunResult
    {
        public Guid RunId { get; set; }
        public bool Safe { get; set; }
        public string Reason { get; set; } = "";
        public List<int> UserIds { get; set; } = new();
        public List<int> SesionIds { get; set; } = new();
        public List<int> AutorizacionIds { get; set; } = new();
        public List<int> EventoIds { get; set; } = new();
        public List<int> CodigoIds { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
    }
}
