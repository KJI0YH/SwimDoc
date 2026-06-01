namespace UI.Services;

public interface IPagingSettingsService
{
    event Action<PagingPage>? PageSizeChanged;

    int GetPageSize(PagingPage page);

    int SetPageSize(PagingPage page, int pageSize);

    int GetDefaultPageSize(PagingPage page);
}
