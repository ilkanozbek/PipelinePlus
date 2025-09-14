using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using MediatR.PipelinePlus.Abstractions;
using Microsoft.Extensions.Logging;



namespace MediatR.PipelinePlus.Behaviors;


public interface IPublishesDomainEvents
{
IReadOnlyCollection<object> DomainEvents { get; }
}


public sealed class OutboxBehavior<TRequest,TResponse>(IOutbox outbox, ILogger<OutboxBehavior<TRequest,TResponse>> logger)
: IPipelineBehavior<TRequest,TResponse>
{
public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
{
var response = await next();
if (response is IPublishesDomainEvents pub && pub.DomainEvents.Count > 0)
{
foreach (var evt in pub.DomainEvents)
await outbox.EnqueueAsync(evt, ct);
logger.LogInformation("[Outbox] Enqueued {Count} events from {ResponseType}", pub.DomainEvents.Count, typeof(TResponse).Name);
}
return response;
}
}