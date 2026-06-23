namespace ServiceLayer.AppSettings;

public interface IAppSettingsStore
{
    AppSettings Get();
    void Update(Action<AppSettings> update);
}
