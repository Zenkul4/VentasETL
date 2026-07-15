using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Core.Entities;
using Core.Interfaces;
using VentasETL.Core.ResultPattern;

namespace VentasETL.Infrastructure.Services.Extractors;

public class DbClientesExtractor(ILogger<DbClientesExtractor> logger) : IDataExtractor<Cliente>
{
    public async Task<Result<IEnumerable<Cliente>>> ExtractAsync(string basePath, CancellationToken cancellationToken)
    {
        var filePath = Path.Combine(basePath, "Clientes.csv");
        var fullPath = Path.GetFullPath(filePath);
        logger.LogInformation("Intentando leer: {Ruta}", fullPath);

        if (!File.Exists(fullPath))
        {
            logger.LogError("El archivo no existe: {Ruta}", fullPath);
            return Result<IEnumerable<Cliente>>.Failure($"El archivo no existe: {fullPath}");
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

            logger.LogInformation("Clientes extraídos exitosamente: {Cantidad} registros", records.Count);
            return Result<IEnumerable<Cliente>>.Success(records);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fallo detallado al leer Clientes.csv desde {Ruta}", fullPath);
            return Result<IEnumerable<Cliente>>.Failure($"Error leyendo Clientes.csv (DB simulada): {ex.Message}");
        }
    }
}
