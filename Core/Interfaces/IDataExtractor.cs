using VentasETL.Core.ResultPattern;

namespace VentasETL.Core.Interfaces;

public interface IDataExtractor<T>
{
    Task<Result<IEnumerable<T>>> ExtractAsync(CancellationToken cancellationToken);
}
