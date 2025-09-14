using System.Threading;
using System.Threading.Tasks;

namespace MediatR.PipelinePlus.Abstractions;


public interface IOutbox
{
    Task EnqueueAsync(object message, CancellationToken ct = default);
}