using Microsoft.EntityFrameworkCore.Storage;
using VentasETL.Core.Interfaces;

namespace VentasETL.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly VentasDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(VentasDbContext context)
    {
        _context = context;
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
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

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
        _transaction?.Dispose();
    }
}