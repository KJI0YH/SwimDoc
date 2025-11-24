using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace ServiceLayer.HeatService.Exceptions;

public class HeatAllocationException(IImmutableList<ValidationResult> errors) : Exception
{
    public readonly IImmutableList<ValidationResult> Errors = errors;
}