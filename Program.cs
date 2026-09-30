using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

LoadDotEnv();

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(o =>
    {
        o.InvalidModelStateResponseFactory = ctx =>
        {
            var first = ctx.ModelState.Values.SelectMany(v => v.Errors).FirstOrDefault()?.ErrorMessage
                ?? "Solicitud inválida.";
            return new BadRequestObjectResult(new { error = first });
        };
    });
builder.Services.AddProblemDetails();
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p
    .WithOrigins("http://localhost:3000", "http://127.0.0.1:3000", "http://localhost:5500", "http://127.0.0.1:5500")
    .AllowAnyHeader().AllowAnyMethod()));

// Rate limiting: frena fuerza bruta en login/registro/recuperación y spam de códigos.
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.AddFixedWindowLimiter("auth", x =>
    {
        x.PermitLimit = 10;
        x.Window = TimeSpan.FromMinutes(1);
        x.QueueLimit = 0;
    });
    o.AddFixedWindowLimiter("codigos", x =>
    {
        x.PermitLimit = 5;
        x.Window = TimeSpan.FromMinutes(1);
        x.QueueLimit = 0;
    });
});

// BD (todo el acceso vive en Repositories/Models + Ef*).
builder.Services.AddDbContext<MindzyxsDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Mindzyxs")));

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<RegistroOptions>(builder.Configuration.GetSection("Registro"));
builder.Services.Configure<SesionOptions>(builder.Configuration.GetSection("Sesion"));
builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection("Admin"));
builder.Services.Configure<FirebaseOptions>(builder.Configuration.GetSection("Firebase"));
builder.Services.AddHttpClient();
builder.Services.AddSingleton<FirebaseService>();

builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<ICatalogRepository, EfCatalogRepository>();
builder.Services.AddScoped<ICodigoRepository, EfCodigoRepository>();
builder.Services.AddScoped<ISesionRepository, EfSesionRepository>();
builder.Services.AddScoped<IAuthLogRepository, EfAuthLogRepository>();
builder.Services.AddScoped<ICuestionarioRepository, EfCuestionarioRepository>();
builder.Services.AddScoped<ILineaAyudaRepository, EfLineaAyudaRepository>();
builder.Services.AddScoped<IRecomendacionRepository, EfRecomendacionRepository>();
builder.Services.AddScoped<IHistorialRepository, EfHistorialRepository>();
builder.Services.AddScoped<IRecordatorioRepository, EfRecordatorioRepository>();
builder.Services.AddScoped<IChatRepository, EfChatRepository>();
builder.Services.AddScoped<IAdminRepository, EfAdminRepository>();
builder.Services.AddScoped<ITarjetaStorage, FileSystemTarjetaStorage>();
builder.Services.AddScoped<IEmailSender, SmtpEmailSender>();
builder.Services.AddScoped<CatalogService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<VerificationService>();
builder.Services.AddScoped<LoginService>();
builder.Services.AddScoped<PasswordRecoveryService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<AvisoService>();
builder.Services.AddScoped<InvitadoService>();
builder.Services.AddScoped<CuestionarioService>();
builder.Services.AddScoped<RiesgoService>();
builder.Services.AddScoped<RecomendacionService>();
builder.Services.AddScoped<HistorialService>();
builder.Services.AddScoped<ProgresoService>();
builder.Services.AddScoped<RecordatorioService>();
builder.Services.AddScoped<PerfilService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddScoped<PsicologoService>();
builder.Services.AddScoped<ObservacionesService>();

// Swagger: documentación interactiva en /swagger (Swagger UI) y spec en /swagger/v1/swagger.json.
builder.Services.AddSwaggerGen(o =>
{
    o.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Mindzyxs API",
        Version = "v1",
        Description = """
            API REST de Mindzyxs 1.1 — back-end (ASP.NET Core + EF Core + SQL Server `SANTI/Mindzyxs`).

            Cubre:
            - **PB-01**: Registro de usuario (Paciente/Psicólogo) con verificación de correo.
            - **PB-02 / PB-03**: Autenticación (login, sesión, logout).
            - **PB-05**: Recuperación de contraseña.
            - **PB-06**: Modo invitado (acceso sin cuenta, panel de invitado, logs de acceso/salida/bloqueos).
            - **PB-07**: Cuestionario emocional del invitado (catálogo dinámico desde BD, inicio/fin al 100% con puntaje y nivel).
            - **PB-08**: Riesgo emocional del invitado (cálculo Bajo/Medio/Alto, alerta si Alto, líneas de ayuda).
            - **PB-09**: Recomendaciones básicas del invitado (lista priorizada por nivel, detalle con pasos).
            - **PB-13**: Historial emocional del paciente (lista con filtro de fechas, detalle con nota/respuestas/apoyo).
            - **PB-14**: Progreso de pacientes autorizados (promedio, tendencia, semanas, distribución, serie).
            - **PB-15**: Recordatorios (frecuencia/horario/días, upsert, avisos de hoy para polling).
            - **PB-31**: Chat realtime (token Firebase para el front; reglas en `Firebase/database.rules.json`).
            - **PB-27**: Administración de BD (respaldos `.bak`, descarga y mantenimiento).
            - **PB-04**: Mensaje aclaratorio (aviso de que el sistema no realiza diagnósticos clínicos).

            **Autenticación**
            El `POST /api/account/login` devuelve un token de sesión. Envía ese token como
            `Authorization: Bearer <token>` en los endpoints que lo requieren.
            Usa el botón **Authorize** para pegarlo y poder probarlos aquí mismo.

            **Ejemplo rápido de login**
            ```
            POST /api/account/login
            { "correo": "admin@mindzyxs.com", "password": "..." }
            ```
            """,
        Contact = new OpenApiContact { Name = "Equipo Mindzyxs" }
    });

    // Usa los comentarios XML de los controladores y DTOs en la documentación.
    var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
    if (File.Exists(xmlPath)) o.IncludeXmlComments(xmlPath);

    // Esquema de seguridad: Authorization: Bearer <token>
    o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "Token de sesión",
        In = ParameterLocation.Header,
        Description = "Pega el token de sesión obtenido en POST /api/account/login. Ejemplo: `Bearer kpsLqCCh9-VnApQMlPIX9vIbkdY0oCC`"
    });
    o.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});

var app = builder.Build();

// Swagger UI solo en desarrollo: http://localhost:5015/swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(o =>
    {
        o.SwaggerEndpoint("/swagger/v1/swagger.json", "Mindzyxs API v1");
        o.DocumentTitle = "Mindzyxs API — Documentación";
        o.DisplayRequestDuration();
    });
}

app.UseCors();
app.UseRateLimiter();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseHsts();
}
app.UseAuthorization();
app.MapControllers();
app.Run();

// Carga .env local (desarrollo): GOOGLE_APPLICATION_CREDENTIALS, etc.
// No pisa variables de entorno reales ya definidas. Las rutas relativas
// (ej. la clave de Firebase) se resuelven contra la carpeta del .env.
static void LoadDotEnv()
{
    var dir = Directory.GetCurrentDirectory();
    var path = Path.Combine(dir, ".env");
    if (!File.Exists(path))
    {
        dir = AppContext.BaseDirectory;
        path = Path.Combine(dir, ".env");
        if (!File.Exists(path)) return;
    }
    foreach (var raw in File.ReadAllLines(path))
    {
        var line = raw.Trim();
        if (line.Length == 0 || line.StartsWith('#')) continue;
        var idx = line.IndexOf('=');
        if (idx <= 0) continue;
        var key = line[..idx].Trim();
        var val = line[(idx + 1)..].Trim().Trim('"').Trim('\'');
        if (key == "GOOGLE_APPLICATION_CREDENTIALS" && !Path.IsPathRooted(val))
            val = Path.GetFullPath(Path.Combine(dir, val));
        if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable(key)))
            Environment.SetEnvironmentVariable(key, val);
    }
}
