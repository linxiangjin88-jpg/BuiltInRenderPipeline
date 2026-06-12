using System.Text;

namespace Scripts.Framework.Core.Log.Formatters
{
    public class DefaultLogFormatter : ILogFormatter
    {
        private readonly StringBuilder m_builder = new StringBuilder(256);
        public string Format(LogEntry entry)
        {
            if (entry == null)
            {
                return string.Empty;
            }

            var builder = m_builder;
            builder.Clear();
            builder.Append('[');
            builder.Append(entry.TimeStamp.ToString("HH:mm:ss.fff"));
            builder.Append("] [F:");
            builder.Append(entry.FrameCount);
            builder.Append("] [");
            builder.Append(entry.Level);
            builder.Append("] ");
            if (!string.IsNullOrEmpty(entry.Tag))
            {
                builder.Append('[');
                builder.Append(entry.Tag);
                builder.Append("] ");
            }
            builder.Append(entry.Message);
            return builder.ToString();
        }
    }
}