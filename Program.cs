using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
builder.Services.AddOpenApi();
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

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
// Desactivado en dev para que Postman Agent no pierda Authorization en redirect http->https
// app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
