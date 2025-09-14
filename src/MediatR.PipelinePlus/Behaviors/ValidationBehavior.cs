using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;


namespace MediatR.PipelinePlus.Behaviors;


public sealed class ValidationBehavior<TRequest,TResponse>(IEnumerable<IValidator<TRequest>> validators)
: IPipelineBehavior<TRequest,TResponse> where TRequest : notnull
{
public async Task<TResponse> Handle(
TRequest request,
RequestHandlerDelegate<TResponse> next,
CancellationToken ct)
{
if (validators.Any())
{
var context = new ValidationContext<TRequest>(request);
var failures = (await Task.WhenAll(validators.Select(v => v.ValidateAsync(context, ct))))
.SelectMany(r => r.Errors)
.Where(f => f is not null)
.ToList();


if (failures.Count != 0)
throw new ValidationException(failures);
}
return await next();
}
}