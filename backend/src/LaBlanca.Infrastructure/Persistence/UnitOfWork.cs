using LaBlanca.Application.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace LaBlanca.Infrastructure.Persistence;

internal sealed class UnitOfWork(AppDbContext db) : IUnitOfWork
{
    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken)
    {
        // Command disparado dentro de outro Command reaproveita a transação externa.
        if (db.Database.CurrentTransaction is not null)
        {
            return NoopTransaction.Instance;
        }

        return new EfTransaction(await db.Database.BeginTransactionAsync(cancellationToken));
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) => db.SaveChangesAsync(cancellationToken);

    private sealed class EfTransaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

        public Task RollbackAsync(CancellationToken cancellationToken) => transaction.RollbackAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }

    private sealed class NoopTransaction : IUnitOfWorkTransaction
    {
        public static readonly NoopTransaction Instance = new();

        public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task RollbackAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
