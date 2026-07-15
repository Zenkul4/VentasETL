using VentasETL.Core.ResultPattern;

namespace VentasETL.Core.Interfaces;

public interface IETLService
{
    Task<Result> EjecutarProcesoCargaAsync(string directoryPath, CancellationToken cancellationToken);
}