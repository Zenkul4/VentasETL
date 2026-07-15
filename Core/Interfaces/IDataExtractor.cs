using VentasETL.Core.ResultPattern;

namespace Core.Interfaces;

public interface IDataExtractor<T>
{
    Task<Result<IEnumerable<T>>> ExtractAsync(string basePath, CancellationToken cancellationToken);
}
