using System;

namespace PipelinePlus.Attributes;


[AttributeUsage(AttributeTargets.Class)]
public sealed class IdempotentAttribute : Attribute
{
public IdempotentAttribute(string keyFromHeader = "Idempotency-Key", int ttlSeconds = 300)
{ KeyFromHeader = keyFromHeader; TtlSeconds = ttlSeconds; }


public string KeyFromHeader { get; }
public int TtlSeconds { get; }
}