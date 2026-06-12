namespace Scripts.Framework.Core.Log.Formatters
{
    public interface ILogFormatter
    {
        string Format(LogEntry entry);
    }
}