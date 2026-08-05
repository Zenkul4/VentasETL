using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Core.Entities;
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
        var globalTimer = Stopwatch.StartNew();
        logger.LogInformation("Iniciando el pipeline ETL completo con medición de rendimiento global y resiliencia en Staging...");

        // 1. FASE DE EXTRACCIÓN (Multi-Fuente)
        var clientesResult = await clienteExtractor.ExtractAsync(directoryPath, cancellationToken);
        if (clientesResult.IsFailure) logger.LogWarning("Aviso en extracción de clientes: {Error}", clientesResult.Error);

        var productosResult = await productoExtractor.ExtractAsync(directoryPath, cancellationToken);
        if (productosResult.IsFailure) logger.LogWarning("Aviso en extracción de productos: {Error}", productosResult.Error);

        var ventasResult = await ventaExtractor.ExtractAsync(directoryPath, cancellationToken);
        if (ventasResult.IsFailure) logger.LogWarning("Aviso en extracción de ventas: {Error}", ventasResult.Error);

        var listaClientes = (clientesResult.IsSuccess ? clientesResult.Value : []).ToList();
        var listaProductos = (productosResult.IsSuccess ? productosResult.Value : []).ToList();
        var listaVentas = (ventasResult.IsSuccess ? ventasResult.Value : []).ToList();

        int totalClientesExtraidos = listaClientes.Count;
        int totalProductosExtraidos = listaProductos.Count;
        int totalVentasExtraidas = listaVentas.Count;

        logger.LogInformation(
            "Resumen de Extracción -> Clientes: {C}, Productos: {P}, Ventas: {V}",
            totalClientesExtraidos, totalProductosExtraidos, totalVentasExtraidas);

        // Contadores para métricas cuantitativas de Staging
        int insertadosClientes = 0, omitidosClientes = 0;
        int insertadosProductos = 0, omitidosProductos = 0;
        int insertadosVentas = 0, omitidosVentas = 0;

        // 2. FASE DE TRANSFORMACIÓN Y CARGA A STAGING
        try
        {
            await unitOfWork.BeginTransactionAsync();

            // 2.1 Carga a Staging de Clientes (con normalización .Trim())
            logger.LogInformation("Procesando y cargando clientes a Staging...");
            foreach (var cliente in listaClientes)
            {
                var nombreNormalizado = (cliente.Nombre ?? string.Empty).Trim();
                var emailNormalizado = (cliente.Email ?? string.Empty).Trim();
                var regionNormalizada = (cliente.Region ?? string.Empty).Trim();

                var param = new[] {
                    new SqlParameter("@IdCliente", cliente.IdCliente),
                    new SqlParameter("@Nombre", nombreNormalizado),
                    new SqlParameter("@Email", emailNormalizado),
                    new SqlParameter("@Region", regionNormalizada)
                };

                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(
                        "EXEC sp_InsertarCliente @IdCliente, @Nombre, @Email, @Region", param, cancellationToken);
                    insertadosClientes++;
                }
                catch (SqlException ex) when (ex.Number == 50001 || ex.Number == 2627 || ex.Number == 2601)
                {
                    omitidosClientes++;
                    logger.LogWarning("Cliente con IdCliente {IdCliente} ya existe en Staging. Registro omitido por duplicidad (Error {SqlError}).", cliente.IdCliente, ex.Number);
                }
            }

            // 2.2 Carga a Staging de Productos (con normalización .Trim())
            logger.LogInformation("Procesando y cargando productos a Staging...");
            foreach (var producto in listaProductos)
            {
                var nombreNormalizado = (producto.Nombre ?? string.Empty).Trim();
                var categoriaNormalizada = (producto.Categoria ?? string.Empty).Trim();

                var param = new[] {
                    new SqlParameter("@IdProducto", producto.IdProducto),
                    new SqlParameter("@Nombre", nombreNormalizado),
                    new SqlParameter("@Categoria", categoriaNormalizada),
                    new SqlParameter("@Precio", producto.Precio)
                };

                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(
                        "EXEC sp_InsertarProducto @IdProducto, @Nombre, @Categoria, @Precio", param, cancellationToken);
                    insertadosProductos++;
                }
                catch (SqlException ex) when (ex.Number == 50001 || ex.Number == 2627 || ex.Number == 2601)
                {
                    omitidosProductos++;
                    logger.LogWarning("Producto con IdProducto {IdProducto} ya existe en Staging. Registro omitido por duplicidad (Error {SqlError}).", producto.IdProducto, ex.Number);
                }
            }

            // 2.3 Carga a Staging de Ventas (con cálculo explícito de Total y resiliencia)
            logger.LogInformation("Procesando y cargando ventas a Staging...");
            foreach (var venta in listaVentas)
            {
                var idFuenteCalculado = venta.IdFuente <= 0 ? 1 : venta.IdFuente;
                var totalCalculado = venta.Total <= 0 ? (venta.Cantidad * venta.Precio) : venta.Total;

                var param = new[] {
                    new SqlParameter("@IdVenta", venta.IdVenta),
                    new SqlParameter("@IdCliente", venta.IdCliente),
                    new SqlParameter("@IdProducto", venta.IdProducto),
                    new SqlParameter("@IdFuente", idFuenteCalculado),
                    new SqlParameter("@Cantidad", venta.Cantidad),
                    new SqlParameter("@Precio", venta.Precio),
                    new SqlParameter("@Fecha", venta.Fecha),
                    new SqlParameter("@Total", totalCalculado)
                };

                try
                {
                    await dbContext.Database.ExecuteSqlRawAsync(
                        "EXEC sp_InsertarVenta @IdVenta, @IdCliente, @IdProducto, @IdFuente, @Cantidad, @Precio, @Fecha, @Total", 
                        param, cancellationToken);
                    insertadosVentas++;
                }
                catch (SqlException ex) when (ex.Number == 50001 || ex.Number == 2627 || ex.Number == 2601)
                {
                    omitidosVentas++;
                    logger.LogWarning("La venta con IdVenta {IdVenta} ya existe en Staging. Registro omitido por duplicidad (Error {SqlError}).", venta.IdVenta, ex.Number);
                }
            }

            // 3. FASE DE CONSOLIDACIÓN FINAL EN DATA WAREHOUSE
            logger.LogInformation("Ejecutando procedimiento almacenado de consolidación final en Data Warehouse (sp_ETL_CargarDataWarehouse)...");
            await dbContext.Database.ExecuteSqlRawAsync("EXEC sp_ETL_CargarDataWarehouse", cancellationToken);

            await unitOfWork.CommitAsync();
            globalTimer.Stop();

            int totalInsertados = insertadosClientes + insertadosProductos + insertadosVentas;
            int totalOmitidos = omitidosClientes + omitidosProductos + omitidosVentas;
            double tiempoSegundos = globalTimer.Elapsed.TotalSeconds;

            logger.LogInformation(
                """
                ================================================================================
                RESUMEN EJECUTIVO DE EJECUCIÓN DEL PIPELINE ETL
                ================================================================================
                - Extraídos  : Clientes: {ClientesExtraidos} | Productos: {ProductosExtraidos} | Ventas: {VentasExtraidas}
                - Staging    : Insertados: {TotalInsertados} (Clientes: {CIns}, Productos: {PIns}, Ventas: {VIns})
                               Omitidos   : {TotalOmitidos} (Clientes: {COmi}, Productos: {POmi}, Ventas: {VOmi})
                - Rendimiento: Tiempo Total: {TiempoMs} ms ({TiempoSeg:F2} s)
                ================================================================================
                """,
                totalClientesExtraidos, totalProductosExtraidos, totalVentasExtraidas,
                totalInsertados, insertadosClientes, insertadosProductos, insertadosVentas,
                totalOmitidos, omitidosClientes, omitidosProductos, omitidosVentas,
                globalTimer.ElapsedMilliseconds, tiempoSegundos);

            return Result.Success();
        }
        catch (Exception ex)
        {
            await unitOfWork.RollbackAsync();
            globalTimer.Stop();

            logger.LogError(
                ex, 
                "Fallo crítico en el pipeline ETL tras {TiempoMs} ms ({TiempoSeg:F2} s). Se ha realizado Rollback completo.", 
                globalTimer.ElapsedMilliseconds, 
                globalTimer.Elapsed.TotalSeconds);

            return Result.Failure($"Fallo en la fase de carga a BD: {ex.Message}");
        }
    }
}