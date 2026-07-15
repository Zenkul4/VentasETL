using VentasETL.Core.Entities;
using VentasETL.Core.Interfaces;
using VentasETL.Core.ResultPattern;

namespace VentasETL.Infrastructure.Services.Extractors;

public class ApiProductosExtractor : IDataExtractor<Dim_Producto>
{
    public async Task<Result<IEnumerable<Dim_Producto>>> ExtractAsync(CancellationToken cancellationToken)
    {
        // TODO: Implement API extraction logic for Productos
        await Task.CompletedTask;
        return Result<IEnumerable<Dim_Producto>>.Success([]);
    }
}
