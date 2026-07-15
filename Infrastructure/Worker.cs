using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using VentasETL.Core.Interfaces;

namespace VentasETL.Infrastructure;

public class Worker(ILogger<Worker> logger, IServiceProvider serviceProvider, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Worker iniciado a las: {time}", DateTimeOffset.Now);

        using (var scope = serviceProvider.CreateScope())
        {
            var etlService = scope.ServiceProvider.GetRequiredService<IETLService>();

            // Leer la ruta desde appsettings.json
            string rutaCsv = configuration["ETLSettings:DataSourcesPath"]
                             ?? throw new InvalidOperationException("La ruta del CSV no está configurada.");

            logger.LogInformation("Leyendo archivos desde: {ruta}", rutaCsv);

            var resultado = await etlService.EjecutarProcesoCargaAsync(rutaCsv, stoppingToken);

            if (resultado.IsFailure)
            {
                logger.LogError("El proceso ETL falló: {Error}", resultado.Error);
            }
            else
            {
                logger.LogInformation("Proceso ETL concluido con éxito.");
            }
        }

        // Detener la aplicación tras la ejecución
        Environment.Exit(0);
    }
}