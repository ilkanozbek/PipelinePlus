# MediatR.PipelinePlus

Plug-and-play **MediatR Pipeline Behaviors**  
Validation ✅ · Caching ✅ · Idempotency ✅ · Performance Log ✅ · Outbox ✅ · Exception Mapping ✅

> **Target Frameworks:** `net8.0`, `net9.0`  
> **Packages:**  
> - `MediatR.PipelinePlus` (core)  
> - `MediatR.PipelinePlus.Redis` (optional Redis adapter)

---

## Why PipelinePlus?

MediatR is great—but most teams re-implement the same plumbing:

- FluentValidation integration  
- Response caching & **idempotent** commands  
- Consistent error payloads (RFC7807 / ProblemDetails)  
- Performance timing logs  
- Domain events via **outbox**

**PipelinePlus** ships these as ready-to-use MediatR behaviors. Register once, decorate your requests with attributes, done.

---

## Install

```bash
dotnet add package MediatR.PipelinePlus
# optional: distributed cache/idempotency via Redis
dotnet add package MediatR.PipelinePlus.Redis
```

---

## Quick Start (Minimal API)

Copy-paste into a fresh `web` project (or see a ready-to-run sample under `samples/WebApi`).

```csharp
// Program.cs
using FluentValidation;
using MediatR;
using MediatR.PipelinePlus;
using MediatR.PipelinePlus.Abstractions;
using MediatR.PipelinePlus.Implementations;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// core deps
builder.Services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
builder.Services.AddSingleton<IExceptionMapper, DefaultExceptionMapper>();
builder.Services.AddSingleton<IOutbox, ConsoleOutbox>(); // sample outbox

// MediatR + validators
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// enable behaviors
builder.Services.AddMediatRPipelinePlus(o =>
{
    o.UseValidation = true;
    o.UsePerformanceLog = true;
    o.UseCaching = true;
    o.UseIdempotency = true;
    o.UseOutbox = true;
    o.UseExceptionMapping = true;
});

var app = builder.Build();

// (optional) ProblemDetails middleware
app.UseMiddleware<ProblemDetailsMiddleware>();

app.MapPost("/payments", async (CreatePayment cmd, ISender mediator)
    => Results.Ok(await mediator.Send(cmd)));

app.MapGet("/orders", async (string orderId, ISender mediator)
    => Results.Ok(await mediator.Send(new GetOrder(orderId))));

app.Run();

// ---- demo types (place in your app to try quickly) ----
public sealed class ProblemDetailsMiddleware(RequestDelegate next, ILogger<ProblemDetailsMiddleware> logger)
{
    public async Task Invoke(HttpContext http)
    {
        try { await next(http); }
        catch (Exception ex)
        {
            var mapped = http.Items["problem"] as dynamic;
            var status = (int?)mapped?.status ?? StatusCodes.Status500InternalServerError;
            var type   = (string?)mapped?.type   ?? "about:blank";
            var title  = (string?)mapped?.title  ?? "Unhandled error";

            logger.LogError(ex, "Unhandled exception -> {Status}", status);

            http.Response.StatusCode = status;
            http.Response.ContentType = "application/problem+json";
            await http.Response.WriteAsJsonAsync(new { type, title, status, traceId = http.TraceIdentifier });
        }
    }
}

[Idempotent] // repeat with same header → returns first result
public sealed record CreatePayment(string OrderId, decimal Amount) : IRequest<PaymentResult>;

public sealed class CreatePaymentValidator : AbstractValidator<CreatePayment>
{
    public CreatePaymentValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Amount).GreaterThan(0);
    }
}

public sealed class CreatePaymentHandler : IRequestHandler<CreatePayment, PaymentResult>
{
    public async Task<PaymentResult> Handle(CreatePayment r, CancellationToken ct)
    {
        await Task.Delay(50, ct); // simulate I/O
        return new PaymentResult("OK", Guid.NewGuid().ToString())
        {
            DomainEvents = new[] { new PaymentSucceededIntegrationEvent(r.OrderId) }
        };
    }
}

public sealed record PaymentResult(string Status, string PaymentId) : IPublishesDomainEvents
{
    public IReadOnlyCollection<object> DomainEvents { get; init; } = Array.Empty<object>();
}

public sealed record PaymentSucceededIntegrationEvent(string OrderId);

[Cache("order:{OrderId}", 60)] // 60s cache
public sealed record GetOrder(string OrderId) : IRequest<OrderVm>;
public sealed record OrderVm(string Id, decimal Amount, string Status);

public sealed class GetOrderHandler : IRequestHandler<GetOrder, OrderVm>
{
    public Task<OrderVm> Handle(GetOrder r, CancellationToken ct)
        => Task.FromResult(new OrderVm(r.OrderId, 149.90m, "Paid"));
}

// simple Outbox that logs
public sealed class ConsoleOutbox(ILogger<ConsoleOutbox> logger) : IOutbox
{
    public Task EnqueueAsync(object message, CancellationToken ct = default)
    {
        logger.LogInformation("[Outbox] Enqueued: {Type}", message.GetType().Name);
        return Task.CompletedTask;
    }
}
```

> **Using Redis?**
> ```csharp
> using MediatR.PipelinePlus.Redis;
> builder.Services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
> builder.Services.AddSingleton<ICacheProvider, RedisCacheProvider>();
> ```

---

## Attributes

- **`[Cache(keyTemplate, seconds)]`**  
  Caches **query** responses (`IRequest<TResponse>` where `TResponse != Unit`).  
  Use `{PropertyName}` placeholders from the request (e.g., `"customer:{Id}:v2"`).

- **`[Idempotent(header = "Idempotency-Key", ttlSeconds = 300)]`**  
  For **commands**: the first successful response is stored; same header returns it within TTL.

> Tip: Version your cache keys (e.g., `:v2`) when response shape changes.

---

## Behaviors & Options

Enable/disable behaviors via `AddMediatRPipelinePlus`:

```csharp
builder.Services.AddMediatRPipelinePlus(o =>
{
    o.UseValidation = true;        // FluentValidation
    o.UsePerformanceLog = true;    // logs elapsed ms
    o.UseCaching = true;           // [Cache]
    o.UseIdempotency = true;       // [Idempotent]
    o.UseOutbox = true;            // collects IPublishesDomainEvents.DomainEvents
    o.UseExceptionMapping = true;  // IExceptionMapper → ProblemDetails
});
```

---

## Exception Mapping & ProblemDetails

Convert exceptions into RFC7807 metadata via `IExceptionMapper`:

```csharp
public sealed class DefaultExceptionMapper : IExceptionMapper
{
    public (int statusCode, string problemType, string title)? Map(Exception ex) => ex switch
    {
        FluentValidation.ValidationException => (422, "validation_error", "Validation failed"),
        KeyNotFoundException                 => (404, "not_found", "Resource not found"),
        _ => null
    };
}
```

Then return a ProblemDetails payload in a middleware (see the example in **Quick Start**).  
The library’s `ExceptionMappingBehavior` stores mapping hints in `HttpContext.Items["problem"]` before rethrowing—so your middleware can produce a consistent response.

---

## Cache Providers

- **Memory (default)** – `MemoryCacheProvider` (great for single-instance dev/test).  
  ```csharp
  builder.Services.AddMemoryCache();
  builder.Services.AddSingleton<ICacheProvider, MemoryCacheProvider>();
  ```

- **Redis (distributed)** – via `MediatR.PipelinePlus.Redis`  
  ```csharp
  using MediatR.PipelinePlus.Redis;

  builder.Services.AddStackExchangeRedisCache(o => o.Configuration = "localhost:6379");
  builder.Services.AddSingleton<ICacheProvider, RedisCacheProvider>();
  ```

---

## Outbox

Return domain events from your handler’s response by implementing `IPublishesDomainEvents`:

```csharp
public sealed record PaymentResult(...) : IPublishesDomainEvents
{
    public IReadOnlyCollection<object> DomainEvents { get; init; } =
        new[] { new PaymentSucceededIntegrationEvent(orderId) };
}
```

Provide an `IOutbox` implementation (e.g., enqueue to a bus, or simply log in dev).

---

## Try It (cURL)

```bash
# Idempotent command (second call returns cached result)
curl -X POST http://localhost:5000/payments \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: abc123" \
  -d '{"orderId":"O-100","amount":149.9}'

curl -X POST http://localhost:5000/payments \
  -H "Content-Type: application/json" \
  -H "Idempotency-Key: abc123" \
  -d '{"orderId":"O-100","amount":149.9}'

# Cached query (second call within 60s → cache HIT)
curl "http://localhost:5000/orders?orderId=O-100"
curl "http://localhost:5000/orders?orderId=O-100"
```

---

## Suggested Project Structure

```
src/
  MediatR.PipelinePlus/
    Abstractions/
    Attributes/
    Behaviors/
    Implementations/
    Options/
  MediatR.PipelinePlus.Redis/
samples/
  WebApi/
    Features/
    Middleware/
    Services/
tests/
  PipelinePlus.UnitTests/
  PipelinePlus.IntegrationTests/
Directory.Build.props
```

---

## Pack & Publish

```bash
# pack
dotnet pack src/MediatR.PipelinePlus/MediatR.PipelinePlus.csproj -c Release -o ./.nupkg

# publish to NuGet (set your API key)
dotnet nuget push ./.nupkg/MediatR.PipelinePlus.*.nupkg \
  --api-key YOUR_KEY --source https://api.nuget.org/v3/index.json
```
> Bump the `<Version>` for each release. Symbol packages (`.snupkg`) are supported.

---

## FAQ / Troubleshooting

- **“Invalid framework identifier '' ” during restore**  
  Ensure `Directory.Build.props` is at the repo root or add `<TargetFrameworks>` to each `.csproj`.

- **Namespace errors (`PipelinePlus.*` vs `MediatR.PipelinePlus.*`)**  
  Ensure all files use `namespace MediatR.PipelinePlus.*;` or set `<RootNamespace>MediatR.PipelinePlus</RootNamespace>`.

- **Idempotency not hitting?**  
  Include the header used by `[Idempotent]` (default `Idempotency-Key`) and register an `ICacheProvider`.

---

## Roadmap

- Cache key version helpers & hashing utilities  
- Request payload hash guard for idempotency  
- Roslyn analyzer: attribute misuses & best practices  
- More providers (e.g., IDistributedCache generic adapter)

---

## Contributing & License

PRs and issues are welcome!  
Licensed under **MIT**.
