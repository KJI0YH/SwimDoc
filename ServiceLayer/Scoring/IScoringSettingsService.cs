namespace ServiceLayer.Scoring;

public interface IScoringSettingsService
{
    ActiveScoringSettings Current { get; }
    event Action? Changed;
    void SetActive(ActiveScoringSettings settings, bool raiseChanged = true);
}
