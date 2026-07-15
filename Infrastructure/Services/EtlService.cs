using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VentasETL.Core.Interfaces;
using VentasETL.Core.ResultPattern;
using VentasETL.Infrastructure.Data;

namespace VentasETL.Infrastructure.Services;

public class EtlService(VentasDbContext dbContext, ILogger<EtlService> logger, IUnitOfWork unitOfWork) : IETLService
{
    public async Task<Result> EjecutarProcesoCargaAsync(string directoryPath, CancellationToken cancellationToken)
    {
        logger.LogInformation("Iniciando el pipeline ETL completo...");

        try
        {
            await unitOfWork.BeginTransactionAsync();

            // 1. Cargar Catálogos Maestros primero (para no romper las FK)
            await ProcesarClientesAsync(Path.Combine(directoryPath, "Clientes.csv"), cancellationToken);
            await ProcesarProductosAsync(Path.Combine(directoryPath, "Productos.csv"), cancellationToken);

            // 2. Cargar la tabla Transaccional
            await ProcesarVentasAsync(Path.Combine(directoryPath, "Ventas.csv"), cancellationToken);

            await unitOfWork.CommitAsync();
            logger.LogInformation("Pipeline ETL finalizado exitosamente.");

            return Result.Success();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync();
            logger.LogError(ex, "Fallo crítico en el pipeline ETL. Se ha hecho Rollback de todo.");
            return Result.Failure($"Fallo en el pipeline: {ex.Message}");
        }
    }

    private async Task ProcesarClientesAsync(string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath))
        {
            logger.LogWarning("Archivo de clientes no encontrado: {Path}", filePath);
            return;
        }

        logger.LogInformation("--> Procesando Clientes...");
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

        await csv.ReadAsync();
        csv.ReadHeader();

        int procesados = 0;
        while (await csv.ReadAsync())
        {
            var idCliente = csv.GetField<int>("IdCliente");
            var nombre = csv.GetField<string>("Nombre");
            var email = csv.GetField<string>("Email");
            var region = csv.GetField<string>("Region");

            var param = new[] {
                new SqlParameter("@IdCliente", idCliente),
                new SqlParameter("@Nombre", nombre ?? ""),
                new SqlParameter("@Email", email ?? ""),
                new SqlParameter("@Region", region ?? "")
            };

            await dbContext.Database.ExecuteSqlRawAsync(
                "EXEC sp_InsertarCliente @IdCliente, @Nombre, @Email, @Region", param, ct);
            procesados++;
        }
        logger.LogInformation("Clientes procesados: {C}", procesados);
    }

    private async Task ProcesarProductosAsync(string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath))
        {
            logger.LogWarning("Archivo de productos no encontrado: {Path}", filePath);
            return;
        }

        logger.LogInformation("--> Procesando Productos...");
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

        await csv.ReadAsync();
        csv.ReadHeader();

        int procesados = 0;
        while (await csv.ReadAsync())
        {
            var idProducto = csv.GetField<int>("IdProducto");
            var nombre = csv.GetField<string>("Nombre");
            var categoria = csv.GetField<string>("Categoria");
            var precio = csv.GetField<decimal>("Precio");

            var param = new[] {
                new SqlParameter("@IdProducto", idProducto),
                new SqlParameter("@Nombre", nombre ?? ""),
                new SqlParameter("@Categoria", categoria ?? ""),
                new SqlParameter("@Precio", precio)
            };

            await dbContext.Database.ExecuteSqlRawAsync(
                "EXEC sp_InsertarProducto @IdProducto, @Nombre, @Categoria, @Precio", param, ct);
            procesados++;
        }
        logger.LogInformation("Productos procesados: {P}", procesados);
    }

    private async Task ProcesarVentasAsync(string filePath, CancellationToken ct)
    {
        if (!File.Exists(filePath)) throw new FileNotFoundException($"Falta {filePath}");

        logger.LogInformation("--> Procesando Ventas...");
        using var reader = new StreamReader(filePath);
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture) { HasHeaderRecord = true });

        await csv.ReadAsync();
        csv.ReadHeader();

        int procesados = 0, insertados = 0, rechazados = 0;
        var errores = new List<string>();

        while (await csv.ReadAsync())
        {
            procesados++;
            try
            {
                var idVenta = csv.GetField<int>("IdVenta");
                var idCliente = csv.GetField<int>("IdCliente");
                var idProducto = csv.GetField<int>("IdProducto");
                var cantidad = csv.GetField<int>("Cantidad");
                var precio = csv.GetField<decimal>("Precio");
                var fecha = csv.GetField<DateTime>("Fecha");

                if (cantidad <= 0 || precio <= 0) throw new Exception("Cantidad o precio inválidos.");
                var totalCalculado = cantidad * precio;

                var parameters = new[] {
                    new SqlParameter("@IdVenta", idVenta),
                    new SqlParameter("@IdCliente", idCliente),
                    new SqlParameter("@IdProducto", idProducto),
                    new SqlParameter("@IdFuente", 1),
                    new SqlParameter("@Cantidad", cantidad),
                    new SqlParameter("@Precio", precio),
                    new SqlParameter("@Fecha", fecha),
                    new SqlParameter("@Total", totalCalculado)
                };

                await dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC sp_InsertarVenta @IdVenta, @IdCliente, @IdProducto, @IdFuente, @Cantidad, @Precio, @Fecha, @Total",
                    parameters, ct);

                insertados++;
            }
            catch (SqlException ex) when (ex.Number == 50001 || ex.Number == 547)
            {
                rechazados++;
                errores.Add($"Fila {procesados}: Rechazo BD - {ex.Message}");
            }
        }

        logger.LogInformation("Ventas -> Procesados: {P} | Insertados: {I} | Rechazados: {R}", procesados, insertados, rechazados);
    }
}