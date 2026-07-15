using global::Core.Entities;
using global::Core.Interfaces;
using VentasETL.Core.ResultPattern;

namespace VentasETL.Infrastructure.Services.Extractors;

public class CsvVentasExtractor : IDataExtractor<Venta>
{
    public async Task<Result<IEnumerable<Venta>>> ExtractAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement CSV extraction logic for Ventas
        await Task.CompletedTask;
        return Result<IEnumerable<Venta>>.Success([]);
    }
}
