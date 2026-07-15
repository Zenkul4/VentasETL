using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Core.Entities;
using Core.Interfaces;
using VentasETL.Core.ResultPattern;

namespace VentasETL.Infrastructure.Services.Extractors;

public class CsvVentasExtractor(ILogger<CsvVentasExtractor> logger) : IDataExtractor<Venta>
{
    public async Task<Result<IEnumerable<Venta>>> ExtractAsync(string basePath, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(basePath, "Ventas.csv");
        var fullPath = Path.GetFullPath(filePath);
        logger.LogInformation("Intentando leer: {Ruta}", fullPath);

        if (!File.Exists(fullPath))
        {
            logger.LogError("El archivo no existe: {Ruta}", fullPath);
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
                // Campos calculados que no existen en el CSV de origen
                if (record.IdFuente == 0) record.IdFuente = 1; // Fuente CSV por defecto
                if (record.Total == 0) record.Total = record.Cantidad * record.Precio;

                records.Add(record);
            }

            logger.LogInformation("Ventas extraídas exitosamente: {Cantidad} registros", records.Count);
            return Result<IEnumerable<Venta>>.Success(records);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo detallado al leer Ventas.csv desde {Ruta}", fullPath);
            return Result<IEnumerable<Venta>>.Failure($"Error leyendo Ventas.csv: {ex.Message}");
        }
    }
}
