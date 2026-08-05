using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Core.Entities;
using VentasETL.Core.Configurations;
using VentasETL.Core.Interfaces;
using VentasETL.Core.ResultPattern;
using VentasETL.Infrastructure.Data;

namespace VentasETL.Infrastructure.Services.Extractors;

public class DbClientesExtractor(
    VentasDbContext dbContext,
    IOptions<EtlOptions> etlOptions,
    ILogger<DbClientesExtractor> logger) : IDataExtractor<Cliente>
{
    public async Task<Result<IEnumerable<Cliente>>> ExtractAsync(string basePath, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();

        logger.LogInformation("Iniciando extracción de Clientes desde la Base de Datos relacional...");

        try
        {
            // Extracción relacional vía EF Core
            var clientesDb = await dbContext.Clientes
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (clientesDb.Count > 0)
            {
                timer.Stop();
                logger.LogInformation(
                    "Extracción de Clientes (Base de Datos) completada exitosamente. Registros: {Cantidad}, Tiempo: {TiempoMs} ms",
                    clientesDb.Count,
                    timer.ElapsedMilliseconds);

                return Result<IEnumerable<Cliente>>.Success(clientesDb);
            }

            logger.LogWarning("La tabla Clientes en la BD no contiene registros. Ejecutando fallback a archivo local Clientes.csv...");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Fallo al conectar o consultar la base de datos relacional de Clientes. Ejecutando fallback a archivo local Clientes.csv...");
        }

        // Fallback resguardado a archivo CSV de Clientes
        return await ExtraerDesdeArchivoFallbackAsync(basePath, timer, cancellationToken);
    }

    private async Task<Result<IEnumerable<Cliente>>> ExtraerDesdeArchivoFallbackAsync(string basePath, Stopwatch timer, CancellationToken cancellationToken)
    {
        var directory = string.IsNullOrWhiteSpace(basePath) 
            ? etlOptions.Value.DataSourcesPath 
            : basePath;

        var csvConfigPath = etlOptions.Value.CsvFiles.ClientesPath;
        var fileName = string.IsNullOrWhiteSpace(csvConfigPath) ? "Clientes.csv" : Path.GetFileName(csvConfigPath);
        var filePath = Path.Combine(directory, fileName);
        var fullPath = Path.GetFullPath(filePath);

        logger.LogInformation("Ejecutando extracción fallback de Clientes desde CSV: {Ruta}", fullPath);

        if (!File.Exists(fullPath))
        {
            timer.Stop();
            logger.LogError("Fallback fallido: El archivo local de Clientes no existe en la ruta {Ruta}", fullPath);
            return Result<IEnumerable<Cliente>>.Failure($"No se pudo obtener clientes de la BD ni del archivo local fallback: {fullPath}");
        }

        try
        {
            using var reader = new StreamReader(fullPath);
            using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true,
                HeaderValidated = null,
                MissingFieldFound = null
            });

            var records = new List<Cliente>();
            await foreach (var record in csv.GetRecordsAsync<Cliente>(cancellationToken))
            {
                records.Add(record);
            }

            timer.Stop();
            logger.LogInformation(
                "Extracción Fallback de Clientes (CSV) completada con éxito. Registros: {Cantidad}, Tiempo Total: {TiempoMs} ms",
                records.Count,
                timer.ElapsedMilliseconds);

            return Result<IEnumerable<Cliente>>.Success(records);
        }
        catch (Exception ex)
        {
            timer.Stop();
            logger.LogError(ex, "Error al procesar el archivo fallback de Clientes en {Ruta}", fullPath);
            return Result<IEnumerable<Cliente>>.Failure($"Error en fallback de Clientes.csv: {ex.Message}");
        }
    }
}
