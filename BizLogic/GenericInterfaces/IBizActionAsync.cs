using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;

namespace BizLogic.GenericInterfaces;

public interface IBizActionAsync<in TIn, TOut>
{
    IImmutableList<ValidationResult> Errors { get; }
    bool HasErrors { get; }
    Task<TOut> ActionAsync(TIn dto);
}