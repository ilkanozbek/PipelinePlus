using System;
using System.Threading;
using System.Threading.Tasks;

public interface ICacheProvider
{
Task<T> GetAsync<T>(string key, CancellationToken ct = default);
Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken ct = default);
}