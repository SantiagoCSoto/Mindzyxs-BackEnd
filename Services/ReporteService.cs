using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
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

        var paciente = await usuarios.FindByIdAsync(idPaciente, ct)
            ?? throw new InvalidOperationException("Paciente no encontrado.");
        var eventos = await historial.GetEventosAsync(idPaciente, null, null, ct);
        var niveles = await historial.GetNivelesAsync(ct);

        var puntajes = eventos.Where(e => e.PuntajeTotal.HasValue).Select(e => (double)e.PuntajeTotal!.Value).ToList();
        var promedio = puntajes.Count > 0 ? Math.Round(puntajes.Average(), 2) : 0;
        string tendencia = "—";
        if (puntajes.Count >= 2)
        {
            var mitad = puntajes.Count / 2;
            var antes = puntajes.Take(mitad).Average();
            var despues = puntajes.Skip(mitad).Average();
            tendencia = despues - antes > 0.5 ? "Sube" : antes - despues > 0.5 ? "Baja" : "Estable";
        }
        var distribucion = new Dictionary<string, int>();
        foreach (var e in eventos.Where(e => e.IdNivelRiesgo.HasValue && niveles.TryGetValue(e.IdNivelRiesgo.Value, out _)))
        {
            var nivel = niveles[e.IdNivelRiesgo!.Value];
            distribucion[nivel] = distribucion.TryGetValue(nivel, out var c) ? c + 1 : 1;
        }
        var nivelFrecuente = distribucion.Count > 0
            ? distribucion.OrderByDescending(kv => kv.Value).First().Key : "—";

        QuestPDF.Settings.License = LicenseType.Community;
        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.Header().Column(c =>
                {
                    c.Item().Text("Mindzyxs - Reporte de bienestar emocional").SemiBold().FontSize(16).FontColor(Colors.Blue.Darken2);
                    c.Item().Text("Herramienta de apoyo. No constituye diagnóstico clínico.").Italic().FontSize(9).FontColor(Colors.Grey.Darken1);
                    c.Item().LineHorizontal(1);
                });
                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    // --- Datos del paciente ---
                    col.Item().Text("Datos del paciente").Bold().FontSize(13);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(2); });
                        t.Cell().Text("Nombre").Bold(); t.Cell().Text($"{paciente.Nombre} {paciente.Apellido}".Trim());
                        t.Cell().Text("Correo").Bold(); t.Cell().Text(paciente.Correo);
                        t.Cell().Text("Fecha de nacimiento").Bold(); t.Cell().Text(paciente.FechaNacimiento?.ToString("dd/MM/yyyy") ?? "—");
                        t.Cell().Text("Fecha de generación").Bold(); t.Cell().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                    });

                    // --- Estadísticas ---
                    col.Item().Text("Estadísticas").Bold().FontSize(13);
                    col.Item().Table(t =>
                    {
                        t.ColumnsDefinition(c => { c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); c.RelativeColumn(1); });
                        t.Header(h =>
                        {
                            h.Cell().Background(Colors.Grey.Lighten2).Text("Total registros").Bold();
                            h.Cell().Background(Colors.Grey.Lighten2).Text("Promedio").Bold();
                            h.Cell().Background(Colors.Grey.Lighten2).Text("Tendencia").Bold();
                            h.Cell().Background(Colors.Grey.Lighten2).Text("Nivel frecuente").Bold();
                        });
                        t.Cell().Text(eventos.Count.ToString());
                        t.Cell().Text(puntajes.Count > 0 ? promedio.ToString() : "—");
                        t.Cell().Text(tendencia);
                        t.Cell().Text(nivelFrecuente);
                    });
                    if (puntajes.Count > 0)
                    {
                        col.Item().Text($"Puntaje mínimo: {puntajes.Min()} | Máximo: {puntajes.Max()}").FontSize(10);
                    }

                    // --- Distribución por nivel (barras) ---
                    if (distribucion.Count > 0)
                    {
                        col.Item().Text("Distribución por nivel de riesgo").Bold().FontSize(12);
                        var maxDist = distribucion.Values.Max();
                        foreach (var kv in distribucion.OrderByDescending(x => x.Value))
                        {
                            var pct = maxDist > 0 ? (double)kv.Value / maxDist : 0;
                            col.Item().Row(r =>
                            {
                                r.RelativeItem(1).Text(kv.Key);
                                r.RelativeItem(3).PaddingVertical(2).Background(Colors.Grey.Lighten3).Height(12).Width((int)(pct * 100)).Background(Colors.Blue.Medium);
                                r.ConstantItem(40).AlignRight().Text(kv.Value.ToString());
                            });
                        }
                    }

                    // --- Historial de registros (tabla) ---
                    col.Item().Text("Historial de registros emocionales").Bold().FontSize(13);
                    if (eventos.Count == 0)
                    {
                        col.Item().Text("Sin registros en el rango.").Italic();
                    }
                    else
                    {
                        col.Item().Table(t =>
                        {
                            t.ColumnsDefinition(c =>
                            {
                                c.ConstantColumn(70);
                                c.ConstantColumn(50);
                                c.ConstantColumn(60);
                                c.RelativeColumn(3);
                            });
                            t.Header(h =>
                            {
                                h.Cell().Background(Colors.Grey.Lighten2).Text("Fecha").Bold();
                                h.Cell().Background(Colors.Grey.Lighten2).Text("Puntaje").Bold();
                                h.Cell().Background(Colors.Grey.Lighten2).Text("Nivel").Bold();
                                h.Cell().Background(Colors.Grey.Lighten2).Text("Nota").Bold();
                            });
                            foreach (var e in eventos.OrderBy(x => x.FechaEvento))
                            {
                                var nivel = e.IdNivelRiesgo.HasValue && niveles.TryGetValue(e.IdNivelRiesgo.Value, out var n) ? n : "—";
                                t.Cell().Text(e.FechaEvento.ToString("dd/MM/yyyy"));
                                t.Cell().Text(e.PuntajeTotal?.ToString() ?? "—");
                                t.Cell().Text(nivel);
                                t.Cell().Text(string.IsNullOrWhiteSpace(e.Nota) ? "—" : e.Nota!).FontSize(9);
                            }
                        });
                    }

                    col.Item().LineHorizontal(1);
                    col.Item().Text("Generado por Mindzyxs. Herramienta de apoyo, no diagnóstico.").FontSize(8).Italic();
                });
                page.Footer().AlignCenter().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
            });
        });
        return doc.GeneratePdf();
    }
}
