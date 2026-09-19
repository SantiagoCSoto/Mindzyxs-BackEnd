using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;
using Mindzyxs_API.Services;
using Mindzyxs_API.Utilities;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddCors(o => o.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// BD (todo el acceso vive en Repositories/Models + Ef*).
builder.Services.AddDbContext<MindzyxsDbContext>(o =>
    o.UseSqlServer(builder.Configuration.GetConnectionString("Mindzyxs")));

builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Smtp"));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection("Storage"));
builder.Services.Configure<RegistroOptions>(builder.Configuration.GetSection("Registro"));
builder.Services.Configure<SesionOptions>(builder.Configuration.GetSection("Sesion"));
builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection("Admin"));

builder.Services.AddScoped<IUserRepository, EfUserRepository>();
builder.Services.AddScoped<ICatalogRepository, EfCatalogRepository>();
builder.Services.AddScoped<ICodigoRepository, EfCodigoRepository>();
builder.Services.AddScoped<ISesionRepository, EfSesionRepository>();
builder.Services.AddScoped<IAuthLogRepository, EfAuthLogRepository>();
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

// Swagger UI siempre disponible: http://localhost:5015/swagger
app.UseSwagger();
app.UseSwaggerUI(o =>
{
    o.SwaggerEndpoint("/swagger/v1/swagger.json", "Mindzyxs API v1");
    o.DocumentTitle = "Mindzyxs API — Documentación";
    o.DisplayRequestDuration();
});

app.UseCors();
// Desactivado en dev para que Postman Agent no pierda Authorization en redirect http->https
// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
