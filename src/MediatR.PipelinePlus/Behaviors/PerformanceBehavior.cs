using MediatR;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;


namespace MediatR.PipelinePlus.Behaviors;


public sealed class PerformanceBehavior<TRequest,TResponse>(ILogger<PerformanceBehavior<TRequest,TResponse>> logger)
: IPipelineBehavior<TRequest,TResponse>
{
public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken ct)
{
var sw = Stopwatch.StartNew();
try { return await next(); }
finally
{
sw.Stop();
logger.LogInformation("[MediatR] {Request} handled in {Elapsed} ms", typeof(TRequest).Name, sw.ElapsedMilliseconds);
}
}
}