namespace BizLogic.GenericInterfaces;

public sealed class NullBizLog : IBizLog
{
    public static readonly NullBizLog Instance = new();

    public void Info(string message)
    {
    }

    public void Warning(string message)
    {
    }
}
