using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using BizLogic.GenericInterfaces;
using DataLayer.EfCore;

namespace ServiceLayer.BizRunners;

public class RunnerWriteDbWithValidation<TIn, TOut>
{
    private readonly IBizAction<TIn, TOut> _actionClass;
    private readonly EfCoreContext _context;

    public RunnerWriteDbWithValidation(IBizAction<TIn, TOut> actionClass, EfCoreContext context)
    {
        _actionClass = actionClass;
        _context = context;
    }

    public IImmutableList<ValidationResult> Errors { get; private set; }
    public bool HasErrors => Errors.Any();

    public TOut RunAction(TIn dataIn)
    {
        var result = _actionClass.Action(dataIn);
        Errors = _actionClass.Errors;
        if (!HasErrors)
        {
            Errors = _context.SaveChangesWithValidation()
                .ToImmutableList();
        }

        return result;
    }
}