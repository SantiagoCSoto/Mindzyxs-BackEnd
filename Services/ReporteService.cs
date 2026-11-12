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
    private static readonly string ColorPrimario = "#6ECFD4";
    private static readonly string ColorPrimarioFuerte = "#0E7490";
    private static readonly string ColorApoyo = "#6B5BCE";
    private static readonly string ColorTexto = "#1E293B";
    private static readonly string ColorMudo = "#475569";
    private static readonly string ColorFondo = "#F8FAFC";
    private static readonly string ColorBorde = "#E2E8F0";
    private static readonly string ColorSuperficie = "#FFFFFF";

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
                page.DefaultTextStyle(t => t.FontSize(10).FontColor(ColorTexto));

                // ===== HEADER (estilo tarjeta email) =====
                page.Header().Column(c =>
                {
                    c.Item().Background(ColorPrimarioFuerte).Padding(14).Row(r =>
                    {
                        r.RelativeItem().Column(cc =>
                        {
                            cc.Item().Text("Mindzyxs").Bold().FontSize(20).FontColor(Colors.White);
                            cc.Item().Text("Reporte de bienestar emocional").FontSize(11).FontColor(Colors.White);
                        });
                        r.ConstantItem(100).AlignRight().Column(cc =>
                        {
                            cc.Item().Text(DateTime.Now.ToString("dd/MM/yyyy")).FontSize(9).FontColor(Colors.White);
                            cc.Item().Text(DateTime.Now.ToString("HH:mm")).FontSize(9).FontColor(Colors.White);
                        });
                    });
                    c.Item().Background(ColorPrimario).PaddingVertical(3).PaddingHorizontal(14)
                        .Text("Herramienta de apoyo. No constituye diagnóstico clínico.").Italic().FontSize(8).FontColor(Colors.White);
                });

                page.Content().Column(col =>
                {
                    col.Spacing(10);

                    // ===== TARJETA: DATOS DEL PACIENTE =====
                    col.Item().Background(ColorFondo).Border(1).BorderColor(ColorBorde).CornerRadius(8).Padding(12).Column(cc =>
                    {
                        cc.Item().Text("Datos del paciente").Bold().FontSize(13).FontColor(ColorPrimarioFuerte);
                        cc.Item().PaddingTop(6).Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.ConstantColumn(120); c.RelativeColumn(); });
                            void Fila(string k, string v)
                            {
                                t.Cell().PaddingVertical(3).Text(k).Bold().FontSize(9).FontColor(ColorMudo);
                                t.Cell().PaddingVertical(3).Text(v).FontSize(9);
                            }
                            Fila("Nombre", $"{paciente.Nombre} {paciente.Apellido}".Trim());
                            Fila("Correo", paciente.Correo);
                            Fila("Fecha de nacimiento", paciente.FechaNacimiento?.ToString("dd/MM/yyyy") ?? "—");
                            Fila("Fecha de generación", DateTime.Now.ToString("dd/MM/yyyy HH:mm"));
                        });
                    });

                    // ===== TARJETA: ESTADÍSTICAS =====
                    col.Item().Background(ColorFondo).Border(1).BorderColor(ColorBorde).CornerRadius(8).Padding(12).Column(cc =>
                    {
                        cc.Item().Text("Estadísticas").Bold().FontSize(13).FontColor(ColorPrimarioFuerte);
                        cc.Item().PaddingTop(6).Table(t =>
                        {
                            t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); c.RelativeColumn(); });
                            t.Header(h =>
                            {
                                h.Cell().Background(ColorPrimarioFuerte).Padding(6).Text("Total registros").Bold().FontColor(Colors.White).FontSize(9);
                                h.Cell().Background(ColorPrimarioFuerte).Padding(6).Text("Promedio").Bold().FontColor(Colors.White).FontSize(9);
                                h.Cell().Background(ColorPrimarioFuerte).Padding(6).Text("Tendencia").Bold().FontColor(Colors.White).FontSize(9);
                                h.Cell().Background(ColorPrimarioFuerte).Padding(6).Text("Nivel frecuente").Bold().FontColor(Colors.White).FontSize(9);
                            });
                            t.Cell().Padding(6).Text(eventos.Count.ToString()).FontSize(9);
                            t.Cell().Padding(6).Text(puntajes.Count > 0 ? promedio.ToString() : "—").FontSize(9);
                            t.Cell().Padding(6).Text(tendencia).FontSize(9);
                            t.Cell().Padding(6).Text(nivelFrecuente).FontSize(9);
                        });
                        if (puntajes.Count > 0)
                        {
                            cc.Item().PaddingTop(4).Text($"Puntaje mínimo: {puntajes.Min()}  |  Máximo: {puntajes.Max()}").FontSize(9).FontColor(ColorMudo);
                        }
                    });

                    // ===== DISTRIBUCIÓN POR NIVEL (barras) =====
                    if (distribucion.Count > 0)
                    {
                        col.Item().Background(ColorFondo).Border(1).BorderColor(ColorBorde).CornerRadius(8).Padding(12).Column(cc =>
                        {
                            cc.Item().Text("Distribución por nivel de riesgo").Bold().FontSize(12).FontColor(ColorPrimarioFuerte);
                            cc.Item().PaddingTop(6).Table(t =>
                            {
                                t.ColumnsDefinition(c => { c.RelativeColumn(2); c.RelativeColumn(1); });
                                t.Header(h =>
                                {
                                    h.Cell().Background(ColorPrimarioFuerte).Padding(4).Text("Nivel").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(ColorPrimarioFuerte).Padding(4).Text("Cantidad").Bold().FontColor(Colors.White).FontSize(9);
                                });
                                foreach (var kv in distribucion.OrderByDescending(x => x.Value))
                                {
                                    t.Cell().Padding(4).Text(kv.Key).FontSize(9);
                                    t.Cell().Padding(4).Text(kv.Value.ToString()).FontSize(9);
                                }
                            });
                        });
                    }

                    // ===== HISTORIAL DE REGISTROS (tabla) =====
                    col.Item().Background(ColorFondo).Border(1).BorderColor(ColorBorde).CornerRadius(8).Padding(12).Column(cc =>
                    {
                        cc.Item().Text("Historial de registros emocionales").Bold().FontSize(13).FontColor(ColorPrimarioFuerte);
                        if (eventos.Count == 0)
                        {
                            cc.Item().PaddingTop(6).Text("Sin registros en el rango.").Italic().FontColor(ColorMudo);
                        }
                        else
                        {
                            cc.Item().PaddingTop(6).Table(t =>
                            {
                                t.ColumnsDefinition(c =>
                                {
                                    c.ConstantColumn(75);
                                    c.ConstantColumn(45);
                                    c.ConstantColumn(70);
                                    c.RelativeColumn();
                                });
                                t.Header(h =>
                                {
                                    h.Cell().Background(ColorPrimarioFuerte).Padding(5).Text("Fecha").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(ColorPrimarioFuerte).Padding(5).Text("Puntaje").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(ColorPrimarioFuerte).Padding(5).Text("Nivel").Bold().FontColor(Colors.White).FontSize(9);
                                    h.Cell().Background(ColorPrimarioFuerte).Padding(5).Text("Nota").Bold().FontColor(Colors.White).FontSize(9);
                                });
                                foreach (var e in eventos.OrderBy(x => x.FechaEvento))
                                {
                                    var nivel = e.IdNivelRiesgo.HasValue && niveles.TryGetValue(e.IdNivelRiesgo.Value, out var n) ? n : "—";
                                    t.Cell().Padding(4).Text(e.FechaEvento.ToString("dd/MM/yyyy")).FontSize(9);
                                    t.Cell().Padding(4).Text(e.PuntajeTotal?.ToString() ?? "—").FontSize(9);
                                    t.Cell().Padding(4).Text(nivel).FontSize(9);
                                    t.Cell().Padding(4).Text(string.IsNullOrWhiteSpace(e.Nota) ? "—" : e.Nota!).FontSize(8);
                                }
                            });
                        }
                    });

                    col.Item().PaddingTop(4).Text("Generado por Mindzyxs. Herramienta de apoyo, no diagnóstico clínico.").FontSize(8).Italic().FontColor(ColorMudo);
                });

                page.Footer().Background(ColorFondo).Padding(6).Row(r =>
                {
                    r.RelativeItem().Text("Mindzyxs - Reporte de bienestar emocional").FontSize(8).FontColor(ColorMudo);
                    r.ConstantItem(60).AlignRight().Text(x => { x.CurrentPageNumber(); x.Span(" / "); x.TotalPages(); });
                });
            });
        });
        return doc.GeneratePdf();
    }
}
