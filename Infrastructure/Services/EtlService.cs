using System;
using System.Threading;
using System.Threading.Tasks;
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
        var clientesResult = await clienteExtractor.ExtractAsync(directoryPath, cancellationToken);
        if (clientesResult.IsFailure) logger.LogWarning("Error en extracción de clientes: {Error}", clientesResult.Error);

        var productosResult = await productoExtractor.ExtractAsync(directoryPath, cancellationToken);
        if (productosResult.IsFailure) logger.LogWarning("Error en extracción de productos: {Error}", productosResult.Error);

        var ventasResult = await ventaExtractor.ExtractAsync(directoryPath, cancellationToken);
        if (ventasResult.IsFailure) logger.LogWarning("Error en extracción de ventas: {Error}", ventasResult.Error);

        var listaClientes = clientesResult.IsSuccess ? clientesResult.Value : [];
        var listaProductos = productosResult.IsSuccess ? productosResult.Value : [];
        var listaVentas = ventasResult.IsSuccess ? ventasResult.Value : [];

        logger.LogInformation(
            "Resumen de Extracción -> Clientes: {C}, Productos: {P}, Ventas: {V}",
            listaClientes.Count(), listaProductos.Count(), listaVentas.Count());

        // 2. Fase de Transformación y Carga (Data Warehouse)
        try
        {
            await unitOfWork.BeginTransactionAsync();

            // Carga hacia Staging de Clientes
            logger.LogInformation("Cargando clientes en Staging...");
            foreach (var cliente in listaClientes)
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
            foreach (var producto in listaProductos)
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
            foreach (var venta in listaVentas)
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
                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(
                        "EXEC sp_InsertarVenta @IdVenta, @IdCliente, @IdProducto, @IdFuente, @Cantidad, @Precio, @Fecha, @Total", 
                        param, cancellationToken);
                }
                catch (SqlException ex)
                {
                    if (ex.Number == 50001)
                    {
                        logger.LogWarning("La venta con IdVenta {IdVenta} ya existe, saltando registro (Error 50001).", venta.IdVenta);
                        continue;
                    }
                    throw;
                }
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