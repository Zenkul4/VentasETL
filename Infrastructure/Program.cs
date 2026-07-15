using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Core.Entities;
using Core.Interfaces;
using VentasETL.Core.Interfaces;
using VentasETL.Infrastructure;
using VentasETL.Infrastructure.Data;
using VentasETL.Infrastructure.Services;
using VentasETL.Infrastructure.Services.Extractors;

var builder = Host.CreateApplicationBuilder(args);

// El DbContext (con manejo seguro por si la cadena no existe temporalmente)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? "";
builder.Services.AddDbContext<VentasDbContext>(options =>
    options.UseSqlServer(connectionString));

// El patrón Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// El orquestador principal
builder.Services.AddScoped<IETLService, EtlService>();

// Los extractores de datos
builder.Services.AddScoped<IDataExtractor<Cliente>, DbClientesExtractor>();
builder.Services.AddScoped<IDataExtractor<Producto>, ApiProductosExtractor>();
builder.Services.AddScoped<IDataExtractor<Venta>, CsvVentasExtractor>();

// Las fábricas
builder.Services.AddHttpClient();

// El proceso en segundo plano
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();