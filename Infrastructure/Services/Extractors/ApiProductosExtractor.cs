using System.Diagnostics;
using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Core.Entities;
using VentasETL.Core.Configurations;
using VentasETL.Core.Interfaces;
using VentasETL.Core.ResultPattern;

namespace VentasETL.Infrastructure.Services.Extractors;

public class ApiProductosExtractor(
    IHttpClientFactory httpClientFactory,
    IOptions<EtlOptions> etlOptions,
    ILogger<ApiProductosExtractor> logger) : IDataExtractor<Producto>
{
    public async Task<Result<IEnumerable<Producto>>> ExtractAsync(string basePath, CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        var apiConfig = etlOptions.Value.ApiSettings;
        var endpoint = apiConfig.Endpoint;

        logger.LogInformation("Iniciando extracción de Productos vía API REST HTTP en: {Endpoint}", endpoint);

        try
        {
            var client = httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(apiConfig.TimeoutSeconds > 0 ? apiConfig.TimeoutSeconds : 30);

            var response = await client.GetAsync(endpoint, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var productosApi = await response.Content.ReadFromJsonAsync<IEnumerable<Producto>>(options, cancellationToken);

                if (productosApi != null)
                {
                    var lista = productosApi.ToList();
                    timer.Stop();
                    logger.LogInformation(
                        "Extracción API de Productos completada exitosamente. Registros: {Cantidad}, Tiempo: {TiempoMs} ms",
                        lista.Count,
                        timer.ElapsedMilliseconds);

                    return Result<IEnumerable<Producto>>.Success(lista);
                }
            }

            logger.LogWarning("Respuesta no exitosa de la API REST ({StatusCode}). Iniciando mecanismo de fallback a archivo local Productos.csv...", response.StatusCode);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falló la comunicación con la API REST de Productos en {Endpoint}. Ejecutando fallback a archivo local Productos.csv...", endpoint);
        }

        
        return await ExtraerDesdeArchivoFallbackAsync(basePath, timer, cancellationToken);
    }

    private async Task<Result<IEnumerable<Producto>>> ExtraerDesdeArchivoFallbackAsync(string basePath, Stopwatch timer, CancellationToken cancellationToken)
    {
        var directory = string.IsNullOrWhiteSpace(basePath) 
            ? etlOptions.Value.DataSourcesPath 
            : basePath;

        var csvConfigPath = etlOptions.Value.CsvFiles.ProductosPath;
        var fileName = string.IsNullOrWhiteSpace(csvConfigPath) ? "Productos.csv" : Path.GetFileName(csvConfigPath);
        var filePath = Path.Combine(directory, fileName);
        var fullPath = Path.GetFullPath(filePath);

        logger.LogInformation("Ejecutando extracción fallback de Productos desde CSV: {Ruta}", fullPath);

        if (!File.Exists(fullPath))
        {
            timer.Stop();
            logger.LogError("Fallback fallido: El archivo local de Productos no existe en la ruta {Ruta}", fullPath);
            return Result<IEnumerable<Producto>>.Failure($"No se pudo obtener productos de la API ni del archivo local fallback: {fullPath}");
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

            timer.Stop();
            logger.LogInformation(
                "Extracción Fallback de Productos (CSV) completada con éxito. Registros: {Cantidad}, Tiempo Total: {TiempoMs} ms",
                records.Count,
                timer.ElapsedMilliseconds);

            return Result<IEnumerable<Producto>>.Success(records);
        }
        catch (Exception ex)
        {
            timer.Stop();
            logger.LogError(ex, "Error al procesar el archivo fallback de Productos en {Ruta}", fullPath);
            return Result<IEnumerable<Producto>>.Failure($"Error en fallback de Productos.csv: {ex.Message}");
        }
    }
}
