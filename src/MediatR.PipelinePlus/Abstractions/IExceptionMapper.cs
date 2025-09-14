using System;
using System.Net;


namespace PipelinePlus.Abstractions;


public interface IExceptionMapper
{
    (int statusCode, string problemType, string title)? Map(Exception ex);
}