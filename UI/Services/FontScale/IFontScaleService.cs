namespace UI.Services.FontScale;

public interface IFontScaleService
{
    int CurrentFontSize { get; }
    bool CanDecrease { get; }
    bool CanIncrease { get; }
    event Action<int>? FontSizeChanged;
    int SetFontSize(int fontSize);
    void ApplyCurrent();
}
