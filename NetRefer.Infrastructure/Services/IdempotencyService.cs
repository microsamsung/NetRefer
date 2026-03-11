using NetRefer.Application.Interfaces;
using System.Collections.Concurrent;

namespace NetRefer.Infrastructure.Services;

public class IdempotencyService : IIdempotencyService
{
    private static ConcurrentDictionary<string, int> _store
        = new();

    public Task<bool> ExistsAsync(string key)
    {
        return Task.FromResult(
            _store.ContainsKey(key));
    }

    public Task StoreAsync(string key, int orderId)
    {
        _store.TryAdd(key, orderId);

        return Task.CompletedTask;
    }

}