using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using VentasETL.Core.Interfaces;

namespace VentasETL.Infrastructure;

public class Worker(IServiceScopeFactory scopeFactory, ILogger<Worker> logger, IConfiguration configuration) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Ejecutando iteración del Worker a las: {time}", DateTimeOffset.Now);

            try
            {
                using var scope = scopeFactory.CreateScope();
                var etlService = scope.ServiceProvider.GetRequiredService<IETLService>();

                string rutaCsv = configuration["ETLSettings:DataSourcesPath"] ?? string.Empty;

                if (string.IsNullOrEmpty(rutaCsv))
                {
                    logger.LogError("La ruta de origen no está configurada (ETLSettings:DataSourcesPath).");
                }
                else
                {
                    logger.LogInformation("Iniciando extracción y carga de archivos desde: {ruta}", rutaCsv);

                    var resultado = await etlService.EjecutarProcesoCargaAsync(rutaCsv, stoppingToken);

                    if (resultado.IsFailure)
                    {
                        logger.LogError("El proceso ETL falló en esta iteración: {Error}", resultado.Error);
                    }
                    else
                    {
                        logger.LogInformation("Proceso ETL concluido con éxito en esta iteración.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogCritical(ex, "Fallo crítico no controlado en la iteración del Worker. El servicio no se detendrá.");
            }

            // Esperar 1 minuto antes de la próxima ejecución (evita bombardear la base de datos)
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}