using BizLogic.GenericInterfaces;

namespace ServiceLayer.Logging;

public sealed class AppBizLog(IAppLog appLog) : IBizLog
{
    public void Info(string message) => appLog.Info(message);

    public void Warning(string message) => appLog.Warning(message);
}
