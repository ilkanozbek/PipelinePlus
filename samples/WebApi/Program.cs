using FluentValidation;
using MediatR;
using MediatR.PipelinePlus;
using MediatR.PipelinePlus.Abstractions;
using MediatR.PipelinePlus.Implementations;
using Samples.WebApi.Middleware;
using Samples.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

// Infra
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();

// PipelinePlus baðýmlýlýklarý
builder.Services.AddSingleton<ICacheProvider, MemoryCacheProvider>();      // In-Memory
builder.Services.AddSingleton<IExceptionMapper, DefaultExceptionMapper>(); // 422/404 vb.
builder.Services.AddSingleton<IOutbox, ConsoleOutbox>();                   // demo outbox

// MediatR + Validators (bu assembly)
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// PipelinePlus davranýþlarý
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

// ProblemDetails middleware
app.UseMiddleware<ProblemDetailsMiddleware>();

// Endpoints
app.MapPost("/payments", async (Features.CreatePayment cmd, ISender mediator) =>
{
    var res = await mediator.Send(cmd);
    return Results.Ok(res);
});

app.MapGet("/orders", async (string orderId, ISender mediator) =>
{
    var res = await mediator.Send(new Features.GetOrder(orderId));
    return Results.Ok(res);
});

app.Run();