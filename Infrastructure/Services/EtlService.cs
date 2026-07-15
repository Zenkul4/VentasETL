using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Entities;
using Core.Interfaces;
using VentasETL.Core.Interfaces;
using VentasETL.Core.ResultPattern;
using VentasETL.Infrastructure.Data;

namespace VentasETL.Infrastructure.Services;

public class EtlService(
    VentasDbContext dbContext,
    ILogger<EtlService> logger,
    IUnitOfWork unitOfWork,
    IDataExtractor<Cliente> clienteExtractor,
    IDataExtractor<Producto> productoExtractor,
    IDataExtractor<Venta> ventaExtractor) : IETLService
{
    public async Task<Result> EjecutarProcesoCargaAsync(string directoryPath, CancellationToken cancellationToken)
    {
        logger.LogInformation("Iniciando el pipeline ETL completo (Extracción de Multi-Fuentes)...");

        // 1. Fase de Extracción
        var clientesResult = await clienteExtractor.ExtractAsync(cancellationToken);
        if (clientesResult.IsFailure) return Result.Failure($"Error en extracción de clientes: {clientesResult.Error}");

        var productosResult = await productoExtractor.ExtractAsync(cancellationToken);
        if (productosResult.IsFailure) return Result.Failure($"Error en extracción de productos: {productosResult.Error}");

        var ventasResult = await ventaExtractor.ExtractAsync(cancellationToken);
        if (ventasResult.IsFailure) return Result.Failure($"Error en extracción de ventas: {ventasResult.Error}");

        // 2. Fase de Transformación y Carga (Data Warehouse)
        try
        {
            await unitOfWork.BeginTransactionAsync();

            // Carga hacia Staging de Clientes
            logger.LogInformation("Cargando clientes en Staging...");
            foreach (var cliente in clientesResult.Value)
            {
                var param = new[] {
                    new SqlParameter("@IdCliente", cliente.IdCliente),
                    new SqlParameter("@Nombre", cliente.Nombre ?? ""),
                    new SqlParameter("@Email", cliente.Email ?? ""),
                    new SqlParameter("@Region", cliente.Region ?? "")
                };
                await dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC sp_InsertarCliente @IdCliente, @Nombre, @Email, @Region", param, cancellationToken);
            }

            // Carga hacia Staging de Productos
            logger.LogInformation("Cargando productos en Staging...");
            foreach (var producto in productosResult.Value)
            {
                var param = new[] {
                    new SqlParameter("@IdProducto", producto.IdProducto),
                    new SqlParameter("@Nombre", producto.Nombre ?? ""),
                    new SqlParameter("@Categoria", producto.Categoria ?? ""),
                    new SqlParameter("@Precio", producto.Precio)
                };
                await dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC sp_InsertarProducto @IdProducto, @Nombre, @Categoria, @Precio", param, cancellationToken);
            }

            // Carga hacia Staging de Ventas
            logger.LogInformation("Cargando ventas en Staging...");
            foreach (var venta in ventasResult.Value)
            {
                var param = new[] {
                    new SqlParameter("@IdVenta", venta.IdVenta),
                    new SqlParameter("@IdCliente", venta.IdCliente),
                    new SqlParameter("@IdProducto", venta.IdProducto),
                    new SqlParameter("@IdFuente", venta.IdFuente),
                    new SqlParameter("@Cantidad", venta.Cantidad),
                    new SqlParameter("@Precio", venta.Precio),
                    new SqlParameter("@Fecha", venta.Fecha),
                    new SqlParameter("@Total", venta.Total)
                };
                await dbContext.Database.ExecuteSqlRawAsync(
                    "EXEC sp_InsertarVenta @IdVenta, @IdCliente, @IdProducto, @IdFuente, @Cantidad, @Precio, @Fecha, @Total", 
                    param, cancellationToken);
            }

            // La inserción final en el esquema estrella se delega al stored procedure según la directriz
            logger.LogInformation("Ejecutando consolidación final en Data Warehouse...");
            await dbContext.Database.ExecuteSqlRawAsync("EXEC sp_ETL_CargarDataWarehouse", cancellationToken);

            await unitOfWork.CommitAsync();
            logger.LogInformation("Pipeline ETL finalizado exitosamente.");

            return Result.Success();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync();
            logger.LogError(ex, "Fallo crítico en el pipeline ETL. Se ha hecho Rollback de todo.");
            return Result.Failure($"Fallo en el pipeline (Carga a BD): {ex.Message}");
        }
    }
}