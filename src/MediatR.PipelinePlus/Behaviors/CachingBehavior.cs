using System;
using MediatR;
using Microsoft.Extensions.Logging;
using PipelinePlus.Attributes;
using PipelinePlus.Abstractions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MediatR.PipelinePlus.Attributes;


namespace MediatR.PipelinePlus.Behaviors;


public sealed class CachingBehavior<TRequest,TResponse>(ICacheProvider cache, ILogger<CachingBehavior<TRequest,TResponse>> logger)
: IPipelineBehavior<TRequest,TResponse> where TRequest : notnull
{
public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
{
var attr = typeof(TRequest).GetCustomAttribute<CacheAttribute>();
if (attr is null || typeof(TResponse) == typeof(Unit))
return await next();


var key = BuildKey(attr.KeyTemplate, request);
var cached = await cache.GetAsync<TResponse>(key, ct);
if (cached is not null)
{
logger.LogDebug("[Cache HIT] {Key}", key);
return cached;
}


var response = await next();
await cache.SetAsync(key, response!, TimeSpan.FromSeconds(attr.Seconds), ct);
logger.LogDebug("[Cache SET] {Key} ttl={Ttl}", key, attr.Seconds);
return response;
}


private static string BuildKey(string template, TRequest req)
{
// basit token replacer: {Prop}
var key = template;
foreach (var p in req!.GetType().GetProperties())
{
key = key.Replace("{" + p.Name + "}", p.GetValue(req)?.ToString());
}
return key;
}
}