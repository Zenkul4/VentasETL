using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration; // Necesario para leer appsettings
using VentasETL.Core.Interfaces;

namespace VentasETL.Infrastructure;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration; // Inyectar IConfiguration

    public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Worker iniciado a las: {time}", DateTimeOffset.Now);

        using (var scope = _serviceProvider.CreateScope())
        {
            var etlService = scope.ServiceProvider.GetRequiredService<IETLService>();

            // Leer la ruta desde appsettings.json
            string rutaCsv = _configuration["ETLSettings:DataSourcesPath"]
                             ?? throw new ArgumentNullException("La ruta del CSV no está configurada.");

            _logger.LogInformation("Leyendo archivos desde: {ruta}", rutaCsv);

            var resultado = await etlService.EjecutarProcesoCargaAsync(rutaCsv, stoppingToken);

            if (resultado.IsFailure)
            {
                _logger.LogError("El proceso ETL falló: {Error}", resultado.Error);
            }
            else
            {
                _logger.LogInformation("Proceso ETL concluido con éxito.");
            }
        }

        // Detener la aplicación tras la ejecución
        Environment.Exit(0);
    }
}