using System;

namespace MediatR.PipelinePlus.Attributes;


[AttributeUsage(AttributeTargets.Class)]
public sealed class CacheAttribute : Attribute
{
    public CacheAttribute(string keyTemplate, int seconds)
    { KeyTemplate = keyTemplate; Seconds = seconds; }
    public string KeyTemplate { get; }
    public int Seconds { get; }
}