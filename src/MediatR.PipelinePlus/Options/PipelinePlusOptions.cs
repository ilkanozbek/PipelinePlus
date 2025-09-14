namespace MediatR.PipelinePlus.Options;


public sealed class PipelinePlusOptions
{
public bool UseValidation { get; set; } = true;
public bool UsePerformanceLog { get; set; } = true;
public bool UseCaching { get; set; } = true;
public bool UseIdempotency { get; set; } = true;
public bool UseOutbox { get; set; } = false; 
public bool UseExceptionMapping { get; set; } = true;
}