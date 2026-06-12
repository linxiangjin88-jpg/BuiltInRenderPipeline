using System;
using System.Collections.Generic;
using Scripts.Framework.Core.Log.Appenders;
using Scripts.Framework.Core.Log.Formatters;

namespace Scripts.Framework.Core.Log
{
    public static class GameLog
    {
        private static bool g_sBInit;
        public static void Init(bool enable, LogLevel lv, Func<int> frameProvider)
        {
            if (g_sBInit)
            {
                return;
            }
            
            IsEnabled = enable;
            MinLevel = lv;
            FrameCountProvider = frameProvider;
            g_sBInit = true;
            // 默认添加一个Unity日志输出器
            AddAppender(new UnityLogAppender(new DefaultLogFormatter()));
        }

        public static void Clear()
        {
            g_sBInit = false;
            IsEnabled = false;
            FrameCountProvider = null;
            ClearAppenders();
        }
        
        static GameLog()
        {
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
            MiniLevel = LogLevel.Warning;
#endif
        }
        
        public static bool IsEnabled { get; set; }

        public static LogLevel MinLevel { get; set;} = LogLevel.Debug;

        public static Func<int> FrameCountProvider { get; set; } // 外部驱动

        private static readonly List<ILogAppender> gs_SListAppenders = new List<ILogAppender>();
        public static void AddAppender(ILogAppender appender)
        {
            if (appender != null && !gs_SListAppenders.Contains(appender))
            {
                gs_SListAppenders.Add(appender);
            }
        }
        
        public static void RemoveAppender(ILogAppender appender)
        {
            if (appender != null)
            {
                gs_SListAppenders.Remove(appender);
            }
        }
        
        public static void ClearAppenders()
        {
            gs_SListAppenders.Clear();
        }
        
        public static void Debug(string message, string tag = null)
        =>      Log(LogLevel.Debug, message, tag);
        
        public static void Info(string message, string tag = null)
        =>      Log(LogLevel.Info, message, tag);   
        
        public static void Warning(string message, string tag = null)
        =>      Log(LogLevel.Warning, message, tag);
        
        public static void Error(string message, Exception exception = null, string tag = null)
        =>      Log(LogLevel.Error, message, tag, exception);

        public static void Fatal(string message, Exception exception = null, string tag = null)
        =>      Log(LogLevel.Fatal, message, tag, exception);
        
        private static void Log(LogLevel level, string message, string tag = null, Exception exception = null)
        {
            if (level == LogLevel.None || MinLevel > level || !IsEnabled)
            {
                return;
            }
            
            var entry = new LogEntry(level, tag, message, FrameCountProvider?.Invoke() ?? -1, exception);
            foreach (var appender in gs_SListAppenders)
            {
                appender.Append(entry);
            }
        }
    }
}