using VentasETL.Core.Entities;
using VentasETL.Core.Interfaces;
using VentasETL.Core.ResultPattern;

namespace VentasETL.Infrastructure.Services.Extractors;

public class CsvVentasExtractor : IDataExtractor<Fact_Ventas>
{
    public async Task<Result<IEnumerable<Fact_Ventas>>> ExtractAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement CSV extraction logic for Ventas
        await Task.CompletedTask;
        return Result<IEnumerable<Fact_Ventas>>.Success([]);
    }
}
