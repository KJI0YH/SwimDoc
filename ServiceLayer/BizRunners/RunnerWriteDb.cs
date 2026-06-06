using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using BizLogic.GenericInterfaces;
using DataLayer.EfCore;

namespace ServiceLayer.BizRunners;

public class RunnerWriteDb<TIn, TOut>
{
    private readonly IBizAction<TIn, TOut> _actionClass;
    private readonly EfCoreContext _context;

    public IImmutableList<ValidationResult> Errors => _actionClass.Errors;
    public bool HasErrors => _actionClass.HasErrors;

    public RunnerWriteDb(IBizAction<TIn, TOut> actionClass, EfCoreContext context)
    {
        _context = context;
        _actionClass = actionClass;
    }

    public TOut RunAction(TIn dataIn, bool saveChanges = true)
    {
        var result = _actionClass.Action(dataIn);
        if (saveChanges && !HasErrors)
            _context.SaveChanges();
        return result;
    }
}
