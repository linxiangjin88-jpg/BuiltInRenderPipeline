namespace Scripts.Framework.Core.Log.Appenders
{
    public interface ILogAppender
    {
        void Append(LogEntry entry); 
    }
}