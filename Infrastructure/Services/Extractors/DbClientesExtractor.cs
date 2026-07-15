using global::Core.Entities;
using global::Core.Interfaces;
using VentasETL.Core.ResultPattern;

namespace VentasETL.Infrastructure.Services.Extractors;

public class DbClientesExtractor : IDataExtractor<Cliente>
{
    public async Task<Result<IEnumerable<Cliente>>> ExtractAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement Database extraction logic for Clientes
        await Task.CompletedTask;
        return Result<IEnumerable<Cliente>>.Success([]);
    }
}
