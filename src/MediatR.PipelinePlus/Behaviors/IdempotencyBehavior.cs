using System;
using System.Linq;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using MediatR.PipelinePlus.Attributes;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using PipelinePlus.Attributes;


namespace MediatR.PipelinePlus.Behaviors;


public sealed class IdempotencyBehavior<TRequest,TResponse>(IHttpContextAccessor httpCtx, ICacheProvider cache, ILogger<IdempotencyBehavior<TRequest,TResponse>> logger)
: IPipelineBehavior<TRequest,TResponse> where TRequest : notnull
{
public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
{
var attr = typeof(TRequest).GetCustomAttribute<IdempotentAttribute>();
if (attr is null) return await next();


var key = httpCtx.HttpContext?.Request.Headers[attr.KeyFromHeader].FirstOrDefault();
if (string.IsNullOrWhiteSpace(key)) return await next();


var cacheKey = $"idem:{typeof(TRequest).Name}:{key}";
var cached = await cache.GetAsync<TResponse>(cacheKey, ct);
if (cached is not null)
{
logger.LogInformation("[Idempotency HIT] {Key}", cacheKey);
return cached;
}


var response = await next();
await cache.SetAsync(cacheKey, response!, TimeSpan.FromSeconds(attr.TtlSeconds), ct);
return response;
}
}