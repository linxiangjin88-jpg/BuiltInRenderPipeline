using System;

namespace Scripts.Framework.Core.Log
{
    public sealed class LogEntry
    {
        public LogLevel Level { get; }
        public string Tag{ get; }
        public string Message{ get; }
        public DateTime TimeStamp{ get; }
        public int FrameCount{ get; }
        public Exception Exception{ get; }
        
        public LogEntry(LogLevel level, string tag, string message, int frameCount, Exception exception)
        {
            Level = level;
            Tag = tag;
            Message = message;
            Exception = exception;
            TimeStamp = DateTime.Now;
            FrameCount = frameCount;
        }   
    }
}