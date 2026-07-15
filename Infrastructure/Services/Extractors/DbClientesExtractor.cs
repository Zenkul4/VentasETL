using VentasETL.Core.Entities;
using VentasETL.Core.Interfaces;
using VentasETL.Core.ResultPattern;

namespace VentasETL.Infrastructure.Services.Extractors;

public class DbClientesExtractor : IDataExtractor<Dim_Cliente>
{
    public async Task<Result<IEnumerable<Dim_Cliente>>> ExtractAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement Database extraction logic for Clientes
        await Task.CompletedTask;
        return Result<IEnumerable<Dim_Cliente>>.Success([]);
    }
}
