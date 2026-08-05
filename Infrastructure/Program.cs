using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Core.Entities;
using VentasETL.Core.Configurations;
using VentasETL.Core.Interfaces;
using VentasETL.Infrastructure;
using VentasETL.Infrastructure.Data;
using VentasETL.Infrastructure.Services;
using VentasETL.Infrastructure.Services.Extractors;

var builder = Host.CreateApplicationBuilder(args);

// 1. Configuración de Opciones Fuertemente Tipadas (IOptions<EtlOptions>)
builder.Services.Configure<EtlOptions>(
    builder.Configuration.GetSection(EtlOptions.SectionName));

// 2. Configuración del DbContext para la base de datos principal
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
builder.Services.AddDbContext<VentasDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Registro del patrón Unit of Work y servicios core
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IETLService, EtlService>();

// 4. Registro de IHttpClientFactory para el consumo seguro y eficiente de API REST
builder.Services.AddHttpClient();

// 5. Registro de extractores con ciclo de vida Scoped
builder.Services.AddScoped<IDataExtractor<Cliente>, DbClientesExtractor>();
builder.Services.AddScoped<IDataExtractor<Producto>, ApiProductosExtractor>();
builder.Services.AddScoped<IDataExtractor<Venta>, CsvVentasExtractor>();

// 6. Proceso en segundo plano (Worker)
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();