using Mindzyxs_API.Dtos;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Utilities;

namespace Mindzyxs_API.Services;

public class PsicologoCuestionariosService(
    ICuestionarioRepository cuestionarios,
    IUserRepository users,
    ICatalogRepository catalogs,
    ISesionRepository sesiones,
    IAuthLogRepository authLogs)
{
    // PB-20 HU1-PB20: crea el cuestionario Personalizado (Borrador o Publicado)
    // con preguntas, opciones con valor e intervalos de riesgo configurables.
    public async Task<CuestionarioCreadoDto> CrearAsync(string? tokenRaw, CuestionarioCrearDto dto, string? ip, CancellationToken ct = default)
    {
        var yo = await RequirePsicologoAsync(tokenRaw, ct);

        if (string.IsNullOrWhiteSpace(dto.Titulo))
            throw new InvalidOperationException("El título es obligatorio.");
        if (dto.Preguntas is null || dto.Preguntas.Count == 0)
            throw new InvalidOperationException("Debes agregar al menos una pregunta.");

        var preguntas = new List<(PreguntaRow, List<OpcionPreguntaRow>)>();
        var ordenP = 1;
        foreach (var p in dto.Preguntas)
        {
            if (string.IsNullOrWhiteSpace(p.Texto))
                throw new InvalidOperationException("Cada pregunta debe tener texto.");
            if (p.Opciones is null || p.Opciones.Count < 2)
                throw new InvalidOperationException("Cada pregunta debe tener al menos dos opciones.");
            var opciones = new List<OpcionPreguntaRow>();
            var ordenO = 1;
            foreach (var o in p.Opciones)
            {
                if (string.IsNullOrWhiteSpace(o.Texto))
                    throw new InvalidOperationException("Cada opción debe tener texto.");
                if (o.Valor < 0)
                    throw new InvalidOperationException("El valor de cada opción debe ser mayor o igual a cero.");
                opciones.Add(new OpcionPreguntaRow { Texto = o.Texto.Trim(), Valor = o.Valor, Orden = ordenO++ });
            }
            var tipo = string.IsNullOrWhiteSpace(p.Tipo) ? "opcion_multiple" : p.Tipo.Trim();
            preguntas.Add((new PreguntaRow
            {
                Texto = p.Texto.Trim(),
                Orden = ordenP++,
                Tipo = tipo,
                PuntajeMax = opciones.Max(o => o.Valor)
            }, opciones));
        }

        // Puntaje máximo posible (suma de los máximos por pregunta) para validar los intervalos.
        var puntajeMax = preguntas.Sum(x => x.Item2.Max(o => o.Valor));
        string? modo = null;
        int? bajoMax = null;
        int? medioMax = null;
        if (dto.Umbrales is not null)
        {
            if (dto.Umbrales.BajoMax < 0 || dto.Umbrales.MedioMax <= dto.Umbrales.BajoMax)
                throw new InvalidOperationException("Los intervalos de riesgo no son válidos: el límite de Bajo debe ser menor que el de Medio.");
            if (dto.Umbrales.MedioMax >= puntajeMax)
                throw new InvalidOperationException("Los intervalos de riesgo no son válidos: el límite de Medio debe ser menor que el puntaje máximo posible.");
            modo = "suma";
            bajoMax = dto.Umbrales.BajoMax;
            medioMax = dto.Umbrales.MedioMax;
        }

        var fila = new CuestionarioRow
        {
            Titulo = dto.Titulo.Trim(),
            Descripcion = string.IsNullOrWhiteSpace(dto.Descripcion) ? null : dto.Descripcion.Trim(),
            Tipo = "Personalizado",
            Estado = dto.Publicar ? "Publicado" : "Borrador",
            IdCreador = yo.IdUsuario,
            ModoCalculo = modo,
            UmbralBajoMax = bajoMax,
            UmbralMedioMax = medioMax
        };
        var id = await cuestionarios.CrearAsync(fila, preguntas, ct);

        await authLogs.LogAsync(yo.IdUsuario, yo.IdUsuario, LogAcciones.CuestionarioCreado,
            $"Psicólogo:{yo.IdUsuario} creó el cuestionario {id} \"{fila.Titulo}\" ({fila.Estado}" +
            (modo == "suma" ? $", intervalos Bajo≤{bajoMax} Medio≤{medioMax}" : ", regla de riesgo actual") + ")", ip, ct);

        return new CuestionarioCreadoDto { Id = id, Mensaje = "Cuestionario guardado correctamente" };
    }

    // Requiere psicólogo con cuenta activa y aprobada.
    private async Task<UsuarioDto> RequirePsicologoAsync(string? tokenRaw, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(tokenRaw))
            throw new UnauthorizedAccessException("Sesión requerida");
        var sesion = await sesiones.FindByTokenHashAsync(TokenHelper.Hash(tokenRaw.Trim()), ct);
        if (sesion is null || sesion.ExpiraEn < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Sesión requerida");
        var user = await users.FindByIdAsync(sesion.IdUsuario, ct)
            ?? throw new UnauthorizedAccessException("Sesión requerida");
        var rol = await users.GetRolNombreAsync(user.IdRol, ct);
        if (!string.Equals(rol, AppRoles.Psicologo, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("No tienes permisos para acceder a esta función");
        var estado = await users.GetEstadoCuentaNombreAsync(user.IdEstadoCuenta, ct);
        if (!string.Equals(estado, AccountStates.Activa, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("La cuenta no está activa.");
        if (user.IdEstadoAprobacion.HasValue)
        {
            var aprob = await catalogs.GetEstadoNombreAsync(user.IdEstadoAprobacion.Value, ct);
            if (!string.Equals(aprob, AccountStates.Aprobado, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("El psicólogo aún no está aprobado.");
        }
        return user;
    }
}
