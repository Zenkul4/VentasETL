using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Core.Entities;
using Core.Interfaces;
using VentasETL.Core.ResultPattern;

namespace VentasETL.Infrastructure.Services.Extractors;

public class ApiProductosExtractor(ILogger<ApiProductosExtractor> logger) : IDataExtractor<Producto>
{
    public async Task<Result<IEnumerable<Producto>>> ExtractAsync(string basePath, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(basePath, "Productos.csv");
        var fullPath = Path.GetFullPath(filePath);
        logger.LogInformation("Intentando leer: {Ruta}", fullPath);

        if (!File.Exists(fullPath))
        {
            logger.LogError("El archivo no existe: {Ruta}", fullPath);
            return Result<IEnumerable<Producto>>.Failure($"El archivo no existe: {fullPath}");
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

            var records = new List<Producto>();
            await foreach (var record in csv.GetRecordsAsync<Producto>(cancellationToken))
            {
                records.Add(record);
            }

            logger.LogInformation("Productos extraídos exitosamente: {Cantidad} registros", records.Count);
            return Result<IEnumerable<Producto>>.Success(records);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo detallado al leer Productos.csv desde {Ruta}", fullPath);
            return Result<IEnumerable<Producto>>.Failure($"Error leyendo Productos.csv (API simulada): {ex.Message}");
        }
    }
}
