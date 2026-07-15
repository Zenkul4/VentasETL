using Microsoft.EntityFrameworkCore.Storage;
using VentasETL.Core.Interfaces;

namespace VentasETL.Infrastructure.Data;

public class UnitOfWork(VentasDbContext context) : IUnitOfWork
{
    private IDbContextTransaction? _transaction;

    public async Task BeginTransactionAsync()
    {
        _transaction = await context.Database.BeginTransactionAsync();
    }

    public async Task CommitAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
        }
    }

    public async Task RollbackAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
        }
    }

    public Task<int> SaveChangesAsync()
    {
        return context.SaveChangesAsync();
    }

    public void Dispose()
    {
        context.Dispose();
        _transaction?.Dispose();
        GC.SuppressFinalize(this);
    }
}