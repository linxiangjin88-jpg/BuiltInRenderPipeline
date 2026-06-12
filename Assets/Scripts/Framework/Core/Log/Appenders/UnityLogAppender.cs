using Scripts.Framework.Core.Log.Formatters;

namespace Scripts.Framework.Core.Log.Appenders
{
    public class UnityLogAppender : ILogAppender
    {
        private readonly ILogFormatter m_format;
        public UnityLogAppender(ILogFormatter format)
        {
            m_format = format;
        }
        
        public void Append(LogEntry entry)
        {
            string formattedMessage = m_format.Format(entry);
            switch (entry.Level)
            {
                case LogLevel.Debug:
                case LogLevel.Info:
                    UnityEngine.Debug.Log(formattedMessage);
                    break;
                case LogLevel.Warning:
                    UnityEngine.Debug.LogWarning(formattedMessage);
                    break;
                case LogLevel.Error:
                case LogLevel.Fatal:
                    UnityEngine.Debug.LogError(formattedMessage);
                    if (entry.Exception != null)
                    {
                        UnityEngine.Debug.LogException(entry.Exception);
                    }
                    break;
            }
        }
    }
}