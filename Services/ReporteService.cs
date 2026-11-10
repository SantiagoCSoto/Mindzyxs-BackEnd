using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Mindzyxs_API.Services;

public class ReporteService(
    IHistorialRepository historial,
    IUserRepository usuarios,
    ISesionRepository sesiones)
{
    public async Task<byte[]> GenerarReportePacienteAsync(string? tokenRaw, int idPaciente, CancellationToken ct = default)
    {
        // Validar sesión y rol psicólogo
        if (string.IsNullOrWhiteSpace(tokenRaw))
            throw new UnauthorizedAccessException("Sesión requerida");
        var sesion = await sesiones.FindByTokenHashAsync(TokenHelper.Hash(tokenRaw.Trim()), ct);
        if (sesion is null || sesion.ExpiraEn < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Sesión requerida");
        var user = await usuarios.FindByIdAsync(sesion.IdUsuario, ct)
            ?? throw new UnauthorizedAccessException("Sesión requerida");
        var rol = await usuarios.GetRolNombreAsync(user.IdRol, ct);
        if (!string.Equals(rol, AppRoles.Psicologo, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Solo el psicólogo puede generar reportes.");

        // Autorización deshabilitada para pruebas (solo psicólogo)

        var paciente = await usuarios.FindByIdAsync(idPaciente, ct)
            ?? throw new InvalidOperationException("Paciente no encontrado.");
        var eventos = await historial.GetEventosAsync(idPaciente, null, null, ct);
        var niveles = await historial.GetNivelesAsync(ct);

        QuestPDF.Settings.License = LicenseType.Community;
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.Header().Text("Mindzyxs - Reporte de bienestar emocional").SemiBold().FontSize(16);
                page.Content().Column(col =>
                {
                    col.Spacing(10);
                    col.Item().Text($"Paciente: {paciente.Nombre} {paciente.Apellido}").Bold();
                    col.Item().Text($"Correo: {paciente.Correo}");
                    col.Item().Text($"Fecha de generación: {DateTime.Now:dd/MM/yyyy HH:mm}");
                    col.Item().LineHorizontal(1);
                    col.Item().Text("Aviso: este reporte es una herramienta de apoyo y no constituye diagnóstico clínico.").Italic().FontSize(9);

                    col.Item().Text("Resumen").Bold().FontSize(12);
                    var puntajes = eventos.Where(e => e.PuntajeTotal.HasValue).Select(e => (double)e.PuntajeTotal!.Value).ToList();
                    col.Item().Text($"Total de registros: {eventos.Count}");
                    if (puntajes.Count > 0)
                    {
                        var promedio = Math.Round(puntajes.Average(), 2);
                        col.Item().Text($"Promedio: {promedio}");
                        col.Item().Text($"Puntaje mínimo: {puntajes.Min()} | Máximo: {puntajes.Max()}");
                        if (puntajes.Count >= 2)
                        {
                            var mitad = puntajes.Count / 2;
                            var antes = puntajes.Take(mitad).Average();
                            var despues = puntajes.Skip(mitad).Average();
                            var tendencia = despues - antes > 0.5 ? "Sube" : antes - despues > 0.5 ? "Baja" : "Estable";
                            col.Item().Text($"Tendencia: {tendencia}");
                        }
                    }

                    col.Item().Text("Historial de registros").Bold().FontSize(12);
                    foreach (var e in eventos.OrderBy(x => x.FechaEvento))
                    {
                        var nivel = e.IdNivelRiesgo.HasValue && niveles.TryGetValue(e.IdNivelRiesgo.Value, out var n) ? n : "—";
                        col.Item().Text($"{e.FechaEvento:dd/MM/yyyy} — Puntaje: {e.PuntajeTotal?.ToString() ?? "—"} — Nivel: {nivel}");
                        if (!string.IsNullOrWhiteSpace(e.Nota))
                            col.Item().Text($"  Nota: {e.Nota}").FontSize(9).Italic();
                    }

                    col.Item().LineHorizontal(1);
                    col.Item().Text("Generado por Mindzyxs.").FontSize(8).Italic();
                });
                page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
            });
        });
        return doc.GeneratePdf();
    }
}
