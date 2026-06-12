using System;
using System.Collections.Generic;

namespace Scripts.Framework.Core.Pool
{
    public class ObjectPool<T>  where T : class
    {
        private readonly Stack<T> m_pool = new Stack<T>();
        private readonly Func<T> m_createFunc;
        private readonly Action<T> m_actionRent;
        private readonly Action<T> m_actionReturn;
        private readonly Action<T> m_actionDestroy;
        private int m_maxFreeCount;
        
        public int FreeCount => m_pool.Count;
        public int RentedCount { get; private set; }
        public int TotalCount => FreeCount + RentedCount;

        public ObjectPool(Func<T> createFunc, 
            Action<T> actionRent = null,
            Action<T> actionReturn = null, 
            Action<T> actionDestroy = null, 
            int nInitCount = 0, 
            int nMaxFreeCount = -1)
        {
            m_createFunc = createFunc ?? throw new ArgumentNullException(nameof(createFunc));
            m_actionRent = actionRent;
            m_actionReturn = actionReturn;
            m_actionDestroy = actionDestroy;
            m_maxFreeCount = nMaxFreeCount;
            if (nInitCount > 0)
            {
                Prewarm(nInitCount);
            }
        }

        public void SetMaxFreeCount(int nMaxFreeCount)
        {
            m_maxFreeCount = nMaxFreeCount;
            if (m_maxFreeCount < 0)
            {
                return;
            }
            
            while (m_pool.Count > m_maxFreeCount)
            {
                var item = m_pool.Pop();
                m_actionDestroy?.Invoke(item);
            }
        }
        
        public void Prewarm(int nCount)
        {
            if (nCount <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nCount),
                    $"Prewarm count：{nCount} must be positive. Type: {typeof(T)}");
            }

            
            // 有上限
            if (m_maxFreeCount >= 0)
            {
                var canCreateCount  = m_maxFreeCount - m_pool.Count;
                if (canCreateCount <= 0)
                {
                    return;
                }
                
                nCount = Math.Min(nCount, canCreateCount);
            }
            
            for (int i = 0; i < nCount; i++)
            {
                var create = m_createFunc();
                if (create != null)
                {
                    m_pool.Push(create);   
                }
                else
                {
                    throw new InvalidOperationException($"ObjectPool createFunc returned null. Type: {typeof(T)}");
                }
            }
        }
        
        public T Rent()
        {
            T obj = m_pool.Count > 0 ? m_pool.Pop() : m_createFunc();
            if (obj == null)
            {
                throw new InvalidOperationException($"ObjectPool createFunc returned null. Type: {typeof(T)}");
            }
            
            try
            {
                m_actionRent?.Invoke(obj);
                RentedCount++;
                return obj;
            }
            catch
            {
                // 租出初始化失败，尽量销毁，避免对象游离
                m_actionDestroy?.Invoke(obj);
                throw;
            }
        }
        
        public void Return(T obj)
        {
            if (obj == null)
            {
                return;
            }
            
            // 防止 RentedCount 变负（说明归还了未经此池租出的对象）
            if (RentedCount <= 0)
            {
                m_actionDestroy?.Invoke(obj); // 先销毁，避免对象泄漏
                throw new InvalidOperationException($"ObjectPool<{typeof(T)}> Return called more times than Rent.");
            }

            // 对象一旦进入 Return，就已离开"租用"状态：无论后续重置成功还是异常销毁，
            // 都不再属于调用方持有，所以这里必须先减计数，否则异常分支会导致计数虚高泄漏。
            RentedCount--;
            
            try
            {
                m_actionReturn?.Invoke(obj);
            }
            catch
            {
                m_actionDestroy?.Invoke(obj);
                throw;
            }
            
            if (m_maxFreeCount >= 0 && m_pool.Count >= m_maxFreeCount)
            {
                m_actionDestroy?.Invoke(obj);
                return;
            }
            
            m_pool.Push(obj);
        }
        
        /// <summary>
        /// 丢弃一个已出借的对象：不回收进池，直接销毁并扣减租用计数。
        /// 用于池已销毁/关闭后，外部仍持有的对象归还。
        /// </summary>
        public void Discard(T obj)
        {
            if (obj == null)
            {
                return;
            }
            
            m_actionDestroy?.Invoke(obj); // 无论如何先销毁，确保对象不泄漏
            
            if (RentedCount > 0)
            {
                RentedCount--;
            }
        }
        
        // Clear 只处理 Free 对象，不处理已经 Rent 出去的对象。
        public void Clear(bool destroyItem = true)
        {
            while (m_pool.Count > 0)
            {
                var item = m_pool.Pop();
                if (destroyItem)
                {
                    m_actionDestroy?.Invoke(item);   
                }
            }
        }
    }
}