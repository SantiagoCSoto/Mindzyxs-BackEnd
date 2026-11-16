using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Mindzyxs_API;
using Mindzyxs_API.Repositories;
using Mindzyxs_API.Repositories.Models;

namespace Mindzyxs_API.Tests.Integration;

public class FabricaAplicacionWebApiSql : WebApplicationFactory<Program>
{
    private const string ConnStr = "Server=SANTI;Database=Mindzyxs_Test;Integrated Security=True;TrustServerCertificate=True;";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            var descriptors = services.Where(d => d.ServiceType == typeof(MindzyxsDbContext) || d.ServiceType == typeof(DbContextOptions<MindzyxsDbContext>)).ToList();
            foreach (var d in descriptors) services.Remove(d);

            services.AddDbContext<MindzyxsDbContext>(options =>
                options.UseSqlServer(ConnStr));
        });
    }
}
