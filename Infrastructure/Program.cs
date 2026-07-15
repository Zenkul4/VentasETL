using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using VentasETL.Core.Interfaces;
using VentasETL.Infrastructure;
using VentasETL.Infrastructure.Data;
using VentasETL.Infrastructure.Services;

var builder = Host.CreateApplicationBuilder(args);

// 1. Obtener la cadena de conexión desde appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Configurar el DbContext de Entity Framework
builder.Services.AddDbContext<VentasDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Inyección de Dependencias (Domain & Application)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IETLService, EtlService>();

// 4. Registrar la clase Worker como el servicio hospedado principal
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// 5. Iniciar la aplicación
host.Run();