using System;
using System.Collections.Generic;
using Scripts.Framework.Core.Log;

namespace Scripts.Framework.Core.Event
{
    public class EventMgr : Singleton<EventMgr>
    {
        private class ListenerInfo
        {
            public readonly object Owner;
            public readonly Delegate Callback;
            public bool PendingRemove;

            public ListenerInfo(Delegate callback, object owner)
            {
                Owner = owner;
                Callback = callback;
                PendingRemove = false;
            }
        }

        private int m_nDispatchCount;
        private readonly List<Type> m_listRemoveTypes = new List<Type>();
        private readonly Dictionary<Type, List<ListenerInfo>> m_listeners = new Dictionary<Type, List<ListenerInfo>>();
        public void AddListener<T>(Action<T> listener, object owner = null) where T : IEvent
        {
            if (listener == null) return;

            var type = typeof(T);
            if (!m_listeners.TryGetValue(type, out var listInfo))
            {
                // 首次注册该事件类型，创建新列表
                listInfo = new List<ListenerInfo>();
                m_listeners[type] = listInfo;
            }

            foreach (var t in listInfo)
            {
                if (t.Callback.Equals(listener) && t.Owner == owner)
                {
                    // 重复注册
                    t.PendingRemove = false; // 可能之前标记为待移除，现在又重新注册了，取消待移除状态
                    return;
                }
            }

            listInfo.Add(new ListenerInfo(listener, owner));
        }
        
        public void DispatchEvent<T>(T iEvent) where T : IEvent
        {
            if (!m_listeners.TryGetValue(typeof(T), out var listInfo) || listInfo.Count == 0)
                return;
            
            m_nDispatchCount++;
            try
            {
                // 快照当前数量，回调中新增的 listener 不在本次 Dispatch 执行范围内
                int count = listInfo.Count;
                for (int i = 0; i < count; i++)
                {
                    if (!listInfo[i].PendingRemove && listInfo[i].Callback is Action<T> callback)
                    {
                        // 单个监听者异常不应中断后续监听者
                        try
                        {
                            callback.Invoke(iEvent);
                        }
                        catch (Exception e)
                        {
                            GameLog.Error($"Event listener failed. Event: {typeof(T).Name}", e);
                        }
                    }
                }
            }
            finally
            {
                m_nDispatchCount--;
                if (m_nDispatchCount == 0)
                {
                    m_listRemoveTypes.Clear();
                    foreach (var kvp in m_listeners)
                    {
                        var listInfoTemp = kvp.Value;
                        for (int i = listInfoTemp.Count - 1; i >= 0; i--)
                        {
                            if (listInfoTemp[i].PendingRemove)
                            {
                                listInfoTemp.RemoveAt(i);
                            }
                        }

                        if (listInfoTemp.Count == 0)
                        {
                            m_listRemoveTypes.Add(kvp.Key); // 先收集，不在 foreach 内直接删
                        }
                    }

                    foreach (var t in m_listRemoveTypes)
                    {
                        m_listeners.Remove(t);
                    }
                    
                    m_listRemoveTypes.Clear();
                } 
            }
        }
        
        public void RemoveListener<T>(Action<T> listener, object owner = null) where T : IEvent
        {
            if (listener == null) return;

            var type = typeof(T);
            if (!m_listeners.TryGetValue(type, out var listInfo))
            {
                return;
            }

            for (int i = listInfo.Count - 1; i >= 0; i--)
            {
                // 与 AddListener 保持一致，同时匹配 Callback 和 Owner
                if (listInfo[i].Callback.Equals(listener) && listInfo[i].Owner == owner)
                {
                    if (m_nDispatchCount > 0)
                    {   
                        listInfo[i].PendingRemove = true;
                    }
                    else
                    {
                        listInfo.RemoveAt(i);
                    }
                }
            }

            if (m_nDispatchCount == 0 && listInfo.Count == 0)
            {
                m_listeners.Remove(type);
            }
        }
        
        public void RemoveListener(object owner)
        {
            // m_listRemoveTypes 与 DispatchEvent 共享，但 RemoveListener 循环体内
            // 不会触发任何 Dispatch，无重入风险，复用缓冲避免 GC 分配
            m_listRemoveTypes.Clear();
            foreach (var kvp in m_listeners)
            {
                var listInfo = kvp.Value;
                for (int i = listInfo.Count - 1; i >= 0; i--)
                {
                    if (listInfo[i].Owner == owner)
                    {
                        if (m_nDispatchCount > 0)
                        {
                            listInfo[i].PendingRemove = true;
                        }
                        else
                        {
                            listInfo.RemoveAt(i);
                        }
                    }
                }

                if (m_nDispatchCount == 0 && listInfo.Count == 0)
                {
                    m_listRemoveTypes.Add(kvp.Key);
                }
            }

            foreach (var t in m_listRemoveTypes)
            {
                m_listeners.Remove(t);
            }

            m_listRemoveTypes.Clear();
        }
        
        protected override void OnDispose()
        {
            RemoveAllListeners();
        }

        public void RemoveAllListeners()
        {
            if (m_nDispatchCount != 0)
            {
                foreach (var kvp in m_listeners)
                {
                    var listInfo = kvp.Value;
                    for (int i = listInfo.Count - 1; i >= 0; i--)
                    {
                        listInfo[i].PendingRemove = true;
                    }
                }
            }
            else
            {
                m_listeners.Clear();
            }
        }
    }
}