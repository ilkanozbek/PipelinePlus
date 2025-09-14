using System;
using System.Collections.Generic;
using MediatR.PipelinePlus.Abstractions;
using System.Net;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using PipelinePlus.Abstractions;


namespace PipelinePlus.Implementations;


public sealed class DefaultExceptionMapper : IExceptionMapper
{
public (int statusCode, string problemType, string title)? Map(Exception ex) => ex switch
{
ValidationException => (StatusCodes.Status422UnprocessableEntity, "validation_error", "Validation failed"),
KeyNotFoundException => (StatusCodes.Status404NotFound, "not_found", "Resource not found"),
_ => null
};
}