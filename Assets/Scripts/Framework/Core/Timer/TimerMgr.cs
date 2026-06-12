using System;
using UnityEngine;

namespace Scripts.Framework.Core.Timer
{
    public class TimerMgr : MonoSingleton<TimerMgr>
    {
        protected override bool IsDontDestroyOnload() => true;
        
        private readonly TimerScheduler m_scheduler = new TimerScheduler();
        public int Delay(float seconds, Action onComplete, object owner = null, string group = null)
        {
            return m_scheduler.Delay(seconds, onComplete, owner, group);
        }

        public int Repeat(float interval, int repeatCount, Action onTick, object owner = null, string group = null) 
        {
            return m_scheduler.Repeat(interval, repeatCount, onTick, owner, group);
        }

        public int DelayUnscaled(float seconds, Action onComplete, object owner = null, string group = null)
        {
            return m_scheduler.DelayUnscaled(seconds, onComplete, owner, group);
        }

        public int RepeatUnscaled(float interval, int repeatCount, Action onTick, object owner = null, string group = null) 
        {
            return m_scheduler.RepeatUnscaled(interval, repeatCount, onTick, owner, group);
        }
        
        public int DelayFrames(int frameCount, Action onComplete, object owner = null, string group = null)
        {
            return m_scheduler.DelayFrames(frameCount, onComplete, owner, group);
        }
        
        public int RepeatFrames(int frameInterval, int repeatCount, Action onTick, object owner = null, string group = null)
        {
            return m_scheduler.RepeatFrames(frameInterval, repeatCount, onTick, owner, group);
        }

        public int AddUpdate(Action onUpdate, object owner = null, string group = null)
        {
            return m_scheduler.AddUpdate(onUpdate, owner, group);
        }
        
        public int CallLater(Action onComplete, object owner = null, string group = null)
        {
            return m_scheduler.CallLater(onComplete, owner, group);
        }

        public void Cancel(int timerId, bool completed = false)
        {
            m_scheduler.Cancel(timerId, completed);
        }

        public void CancelGroup(string group, bool completed = false)
        {
            m_scheduler.CancelGroup(group, completed);
        }

        public void CancelOwner(object owner, bool completed = false)   
        {
            m_scheduler.CancelOwner(owner, completed);
        }

        public void CancelAll(bool completed = false)
        {
            m_scheduler.CancelAll(completed);
        }

        public void SetPausedById(int timerId, bool paused)
        {
            m_scheduler.SetPausedById(timerId, paused);
        }

        public void SetPausedByGroup(string group, bool paused)
        {
            m_scheduler.SetPausedByGroup(group, paused);
        }

        public void SetPausedByOwner(object owner, bool paused)
        {
            m_scheduler.SetPausedByOwner(owner, paused);
        }

        public void SetAllPaused(bool paused)
        {
            m_scheduler.SetAllPaused(paused);
        }

        private void Update()
        {
            m_scheduler.Update(Time.deltaTime, Time.unscaledDeltaTime, IsOwnerDestroyed);
        }

        private static bool IsOwnerDestroyed(object owner)
        {
            if (owner is UnityEngine.Object unityObj)
            {
                return unityObj == null;
            }

            return false;
        }
        
        protected override void OnDestroy()
        {
            m_scheduler.CancelAll();
            base.OnDestroy();
        }
    }
}