using System.Diagnostics;
using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Core.Entities;
using VentasETL.Core.Configurations;
using VentasETL.Core.Interfaces;
using VentasETL.Core.ResultPattern;

namespace VentasETL.Infrastructure.Services.Extractors;

public class CsvVentasExtractor(
    IOptions<EtlOptions> etlOptions,
    ILogger<CsvVentasExtractor> logger) : IDataExtractor<Venta>
{
    public async Task<Result<IEnumerable<Venta>>> ExtractAsync(string basePath, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();

        var directory = string.IsNullOrWhiteSpace(basePath) 
            ? etlOptions.Value.DataSourcesPath 
            : basePath;

        var csvConfigPath = etlOptions.Value.CsvFiles.VentasPath;
        var fileName = string.IsNullOrWhiteSpace(csvConfigPath) ? "Ventas.csv" : Path.GetFileName(csvConfigPath);
        var filePath = Path.Combine(directory, fileName);
        var fullPath = Path.GetFullPath(filePath);

        logger.LogInformation("Iniciando extracción CSV de Ventas desde: {Ruta}", fullPath);

        if (!File.Exists(fullPath))
        {
            timer.Stop();
            logger.LogError("El archivo de ventas no existe en la ruta especificada: {Ruta}", fullPath);
            return Result<IEnumerable<Venta>>.Failure($"El archivo no existe: {fullPath}");
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

            var records = new List<Venta>();
            await foreach (var record in csv.GetRecordsAsync<Venta>(cancellationToken))
            {
                if (record.IdFuente == 0) record.IdFuente = 1; 
                if (record.Total == 0) record.Total = record.Cantidad * record.Precio;

                records.Add(record);
            }

            timer.Stop();
            logger.LogInformation(
                "Extracción de Ventas (CSV) completada exitosamente. Registros: {Cantidad}, Tiempo: {TiempoMs} ms", 
                records.Count, 
                timer.ElapsedMilliseconds);

            return Result<IEnumerable<Venta>>.Success(records);
        }
        catch (Exception ex)
        {
            timer.Stop();
            logger.LogError(ex, "Fallo detallado al extraer Ventas.csv desde {Ruta} después de {TiempoMs} ms", fullPath, timer.ElapsedMilliseconds);
            return Result<IEnumerable<Venta>>.Failure($"Error procesando Ventas.csv: {ex.Message}");
        }
    }
}
