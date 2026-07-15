using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using global::Core.Entities;
using global::Core.Interfaces;
using VentasETL.Core.Interfaces;
using VentasETL.Infrastructure;
using VentasETL.Infrastructure.Data;
using VentasETL.Infrastructure.Services;
using VentasETL.Infrastructure.Services.Extractors;

var builder = Host.CreateApplicationBuilder(args);

// 1. Obtener la cadena de conexión desde appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

// 2. Configurar el DbContext de Entity Framework
builder.Services.AddDbContext<VentasDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Inyección de Dependencias (Domain & Application)
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IETLService, EtlService>();

// Registrar Extractores Multi-Fuente
builder.Services.AddScoped<IDataExtractor<Producto>, ApiProductosExtractor>();
builder.Services.AddScoped<IDataExtractor<Cliente>, DbClientesExtractor>();
builder.Services.AddScoped<IDataExtractor<Venta>, CsvVentasExtractor>();

// 4. Registrar la clase Worker como el servicio hospedado principal
builder.Services.AddHostedService<Worker>();

var host = builder.Build();

// 5. Iniciar la aplicación
host.Run();