using System;
using System.Collections.Generic;
using Scripts.Framework.Core.Log;

namespace Scripts.Framework.Core.Timer
{
    /// <summary>
    /// Scheduler 美/ˈskɛdʒʊlər/ 调度器
    /// </summary>
    public sealed class TimerScheduler
    {
        private readonly Stack<TimerTask> m_pool = new Stack<TimerTask>(16);
        private readonly List<TimerTask> m_tasks = new List<TimerTask>(32);
        private int m_nIdGen;

        #region 秒数计时
        public int Delay(float seconds, Action onComplete, object owner = null, string group = null)
        {
            return Schedule(TimerMode.Seconds, seconds, 0, 1, owner, group, null, onComplete);
        }
        
        public int Repeat(float interval, int repeatCount, Action onTick, object owner = null, string group = null)
        {
            return Schedule(TimerMode.Seconds, 0, interval, repeatCount, owner, group, onTick);
        }
        
        public int DelayUnscaled(float seconds, Action onComplete, object owner = null, string group = null)
        {
            return Schedule(TimerMode.UnscaledSeconds, seconds, 0, 1, owner, group, null, onComplete);
        }

        public int RepeatUnscaled(float interval, int repeatCount, Action onTick, object owner = null, string group = null)
        {
            return Schedule(TimerMode.UnscaledSeconds, 0, interval, repeatCount, owner, group, onTick);
        }
        
        #endregion

        #region 帧数计时
        public int DelayFrames(int frames, Action onComplete, object owner = null, string group = null)
        {
            return Schedule(TimerMode.Frames, frames, 0, 1, owner, group, null, onComplete);
        }
        
        public int RepeatFrames(int interval, int repeatCount, Action onTick, object owner = null, string group = null)
        {
            return Schedule(TimerMode.Frames, 0, interval, repeatCount, owner, group, onTick);
        }
        
        // 每帧调用
        public int AddUpdate(Action onTick, object owner = null, string group = null)
        {
            return RepeatFrames(1, -1, onTick, owner, group);
        }
        
        // 下一帧调用
        public int CallLater(Action onComplete, object owner = null, string group = null)
        {
            return DelayFrames(1, onComplete, owner, group);
        }
        #endregion

        private static void SafeInvoke(Action action)
        {
            try
            {
                action?.Invoke();   
            }
            catch (Exception e)
            {
                GameLog.Error("Timer callback failed", e);
            }
        }

        private static void MarkCompleted(TimerTask task, bool invokeComplete)
        {
            if (task.IsCompleted)
            {
                return;
            }
            
            task.IsCompleted = true;
            if (invokeComplete && task.OnComplete != null)
            {
                SafeInvoke(task.OnComplete);
            }
        }
        
        #region 控制操作
        // 注意：completed=true 时 MarkCompleted 会同步执行 OnComplete，
        // 回调里可能再次 Schedule（往 m_tasks 尾部 Add），
        // 所以这里必须用快照长度的 for 索引遍历，不能用 foreach（会抛集合修改异常）。
        // 新增的 task 不在本次取消范围内，符合预期。
        public void Cancel(int timerId, bool completed = false)
        {
            int count = m_tasks.Count;
            for (int i = 0; i < count; i++)
            {
                if (m_tasks[i].Id == timerId)
                {
                    MarkCompleted(m_tasks[i], completed);
                    break;
                }
            }
        }
        
        public void CancelGroup(string group, bool completed = false)
        {
            if (string.IsNullOrEmpty(group))
            {
                return;
            }
            
            int count = m_tasks.Count;
            for (int i = 0; i < count; i++)
            {
                if (m_tasks[i].Group == group)
                {
                    MarkCompleted(m_tasks[i], completed);
                }
            }       
        }
        
        public void CancelOwner(object owner, bool completed = false)
        {
            int count = m_tasks.Count;
            for (int i = 0; i < count; i++)
            {
                if (m_tasks[i].Owner == owner)
                {
                    MarkCompleted(m_tasks[i], completed);
                }
            }
        }
        
        public void CancelAll(bool completed = false)
        {
            int count = m_tasks.Count;
            for (int i = 0; i < count; i++)
            {
                MarkCompleted(m_tasks[i], completed);
            }
        }
        
        public void SetPausedById(int timerId, bool paused)
        {
            foreach (var task in m_tasks)
            {
               if (task.Id == timerId)
               {
                   task.IsPause = paused;
                   break;
               }
            }
        }
        
        public void SetPausedByGroup(string group, bool paused)
        {
            foreach (var task in m_tasks)
            {
                if (task.Group == group)
                {
                    task.IsPause = paused;
                }
            }
        }
        
        public void SetPausedByOwner(object owner, bool paused)
        {
            foreach (var task in m_tasks)
            {
                if (task.Owner == owner)
                {
                    task.IsPause = paused;
                }
            }
        }

        public void SetAllPaused(bool paused)
        {
            foreach (var task in m_tasks)
            {
                task.IsPause = paused;
            }
        }

        #endregion
        
        private int Schedule(
            TimerMode mode, 
            float delay, 
            float interval, 
            int repeatCount, 
            object owner = null, 
            string group = null, 
            Action onTick = null, 
            Action onComplete = null)
        {
            if (m_nIdGen == int.MaxValue)
            {
                m_nIdGen = 0;
            }
            
            m_nIdGen++;
            var timer = m_pool.Count > 0 ? m_pool.Pop() : new TimerTask();
            timer.Id = m_nIdGen;
            timer.Mode = mode;
            timer.Delay = delay;
            timer.Interval = interval;
            // repeatCount=0 没有意义（不执行），统一当作单次处理
            timer.RepeatCount = repeatCount == 0 ? 1 : repeatCount;
            timer.Owner = owner;
            timer.Group = group;
            timer.OnTick = onTick;
            timer.OnComplete = onComplete;
            timer.Elapsed = 0;
            timer.IsPause = false;
            timer.IsDelayDone = delay <= 0 && interval > 0;
            timer.IsCompleted = false;
            m_tasks.Add(timer);
            return m_nIdGen;
        }

        public void Update(float deltaTime, float unscaledDeltaTime, Func<object, bool> isOwnerDestroyed = null)
        {
            // 如果已经完成就移除
            for (int i = m_tasks.Count - 1; i >= 0; i--)
            {
                var task = m_tasks[i];
                if (task.IsCompleted)
                {
                    m_tasks.RemoveAt(i);
                    Release(task);
                    continue;
                }

                if (isOwnerDestroyed != null && task.Owner != null && isOwnerDestroyed(task.Owner))
                {
                    m_tasks.RemoveAt(i);
                    Release(task);
                    continue;
                }

                if (task.IsPause)
                {
                    continue;
                }

                switch (task.Mode)
                {
                    case TimerMode.Seconds:
                        task.Elapsed += deltaTime;
                        break;
                    case TimerMode.UnscaledSeconds:
                        task.Elapsed += unscaledDeltaTime;
                        break;
                    case TimerMode.Frames:
                        task.Elapsed += 1;
                        break;
                }
                
                // delay阶段
                if (!task.IsDelayDone)
                {
                    if (task.Delay > task.Elapsed)
                    {
                        continue;
                    } 
                    
                    task.IsDelayDone = true;
                    task.Elapsed -= task.Delay;
                    if (task.Interval <= 0)
                    {
                        task.IsCompleted = true;
                        if (task.OnComplete != null)
                        {
                            SafeInvoke(task.OnComplete);
                        }
                        
                        continue;
                    }
                }
                
                if (task.Interval <= 0)
                {
                    // delay=0 单次 timer 已在 delay 分支处理，正常不会走到这里
                    // 作为保险：不调用 OnComplete（非自然完成），直接回收
                    m_tasks.RemoveAt(i);
                    Release(task);
                    continue;
                }
                
                // interval间隔阶段
                while (task.Elapsed >= task.Interval)  // >= 确保精确时刻触发
                {
                    task.Elapsed -= task.Interval;
                    
                    SafeInvoke(task.OnTick);
                    
                    // OnTick 内部可能调用了 Cancel
                    if (task.IsCompleted)
                    {
                        break;
                    }
                    
                    if (task.RepeatCount > 0)   // > 0：有限次数才减；-1 无限不减
                    {
                        task.RepeatCount--;
                    }
                    
                    if (task.RepeatCount == 0)  // == 0：恰好用完，-1 永远不等于 0
                    {
                        task.IsCompleted = true;
                        SafeInvoke(task.OnComplete);
                        break;
                    }
                }
            }
        }

        private void Release(TimerTask task)
        {
            task.Reset();
            m_pool.Push(task);
        }
    }
}