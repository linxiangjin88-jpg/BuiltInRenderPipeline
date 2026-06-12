using System;

namespace Scripts.Framework.Core.Timer
{
    public enum TimerMode
    {
        Seconds,
        UnscaledSeconds,
        Frames,
    }
    
    internal sealed class TimerTask
    {
        public int Id;
        public object Owner;
        public string Group;
        public TimerMode Mode;
        public float Delay;
        public float Interval; // 间隔    
        public int RepeatCount;
        public Action OnTick;
        public Action OnComplete;
        public bool IsPause;
        public float Elapsed; // 累计
        public bool IsCompleted;
        public bool IsDelayDone;
        
        public void Reset()
        {
            Id = 0;
            Owner = null;
            Group = null;
            Mode = TimerMode.Seconds;
            Delay = 0f;
            Interval = 0f;
            RepeatCount = 0;
            IsPause = false;
            OnTick = null;
            OnComplete = null;
            Elapsed = 0f;
            IsCompleted = false;
            IsDelayDone = false;
        }
    }
}