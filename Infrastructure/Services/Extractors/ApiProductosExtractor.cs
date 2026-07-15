using global::Core.Entities;
using global::Core.Interfaces;
using VentasETL.Core.ResultPattern;

namespace VentasETL.Infrastructure.Services.Extractors;

public class ApiProductosExtractor : IDataExtractor<Producto>
{
    public async Task<Result<IEnumerable<Producto>>> ExtractAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement API extraction logic for Productos
        await Task.CompletedTask;
        return Result<IEnumerable<Producto>>.Success([]);
    }
}
