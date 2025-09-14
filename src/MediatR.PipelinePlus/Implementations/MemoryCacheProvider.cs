using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using MediatR.PipelinePlus.Abstractions;


namespace MediatR.PipelinePlus.Implementations;


public sealed class MemoryCacheProvider(IMemoryCache cache) : ICacheProvider
{
public Task<T> GetAsync<T>(string key, CancellationToken ct = default)
=> Task.FromResult(cache.TryGetValue(key, out var v) ? (T?)v : default);


public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default)
{
cache.Set(key, value, ttl);
return Task.CompletedTask;
}
}