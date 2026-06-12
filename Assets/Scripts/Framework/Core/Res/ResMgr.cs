using System;
using System.Collections.Generic;
using Scripts.Framework.Core.Game;
using Scripts.Framework.Core.Log;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

namespace Scripts.Framework.Core.Res
{
    /// <summary>
    /// 资源管理器（基于 Addressables）：
    /// - 所有加载返回 ResHandle，释放统一走 Release / ReleaseAll
    /// - owner 机制：界面/角色关闭时 ReleaseAll(this) 批量释放，避免逐个记句柄
    /// </summary>
    public class ResMgr : Singleton<ResMgr>, IManager
    {
        private readonly List<ResHandle> m_handles = new List<ResHandle>();

        /// <summary>
        /// 异步加载资源。回调在加载完成时触发；若加载失败或句柄已被提前释放，回调收到 null。
        /// </summary>
        public ResHandle LoadAsync<T>(string key, Action<T> onLoaded = null, object owner = null) where T : Object
        {
            if (string.IsNullOrEmpty(key))
            {
                GameLog.Error("ResMgr.LoadAsync key is null or empty", null, "Res");
                onLoaded?.Invoke(null);
                return null;
            }

            var opHandle = Addressables.LoadAssetAsync<T>(key);
            var resHandle = new ResHandle(key, owner, false, opHandle);
            m_handles.Add(resHandle);

            opHandle.Completed += op =>
            {
                // 加载期间句柄可能已被 Release（如界面提前关闭），不再回调业务
                if (resHandle.IsReleased)
                {
                    return;
                }

                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    SafeInvoke(onLoaded, op.Result, key);
                }
                else
                {
                    GameLog.Error($"ResMgr.LoadAsync failed. key: {key}", op.OperationException, "Res");
                    SafeInvoke(onLoaded, null, key);
                }
            };

            return resHandle;
        }

        /// <summary>
        /// 异步实例化 GameObject。释放该句柄时实例会被销毁。
        /// </summary>
        public ResHandle InstantiateAsync(string key, Transform parent = null, Action<GameObject> onLoaded = null, object owner = null)
        {
            if (string.IsNullOrEmpty(key))
            {
                GameLog.Error("ResMgr.InstantiateAsync key is null or empty", null, "Res");
                onLoaded?.Invoke(null);
                return null;
            }

            var opHandle = Addressables.InstantiateAsync(key, parent);
            var resHandle = new ResHandle(key, owner, true, opHandle);
            m_handles.Add(resHandle);

            opHandle.Completed += op =>
            {
                if (resHandle.IsReleased)
                {
                    return;
                }

                if (op.Status == AsyncOperationStatus.Succeeded)
                {
                    SafeInvoke(onLoaded, op.Result, key);
                }
                else
                {
                    GameLog.Error($"ResMgr.InstantiateAsync failed. key: {key}", op.OperationException, "Res");
                    SafeInvoke(onLoaded, null, key);
                }
            };

            return resHandle;
        }

        public void Release(ResHandle handle)
        {
            if (handle == null || handle.IsReleased)
            {
                return;
            }

            m_handles.Remove(handle);
            ReleaseInternal(handle);
        }

        /// <summary>
        /// 释放某个 owner 名下的所有句柄（界面关闭、角色销毁时调用）
        /// </summary>
        public void ReleaseAll(object owner)
        {
            if (owner == null)
            {
                return;
            }

            for (int i = m_handles.Count - 1; i >= 0; i--)
            {
                if (m_handles[i].Owner == owner)
                {
                    var handle = m_handles[i];
                    m_handles.RemoveAt(i);
                    ReleaseInternal(handle);
                }
            }
        }

        public void ReleaseAll()
        {
            for (int i = m_handles.Count - 1; i >= 0; i--)
            {
                ReleaseInternal(m_handles[i]);
            }

            m_handles.Clear();
        }

        protected override void OnDispose()
        {
            ReleaseAll();
        }

        private static void ReleaseInternal(ResHandle handle)
        {
            handle.IsReleased = true;
            if (!handle.Handle.IsValid())
            {
                return;
            }

            if (handle.IsInstance)
            {
                // ReleaseInstance 会销毁实例并减引用计数
                Addressables.ReleaseInstance(handle.Handle);
            }
            else
            {
                Addressables.Release(handle.Handle);
            }
        }

        private static void SafeInvoke<T>(Action<T> callback, T result, string key) where T : class
        {
            if (callback == null)
            {
                return;
            }

            try
            {
                callback.Invoke(result);
            }
            catch (Exception e)
            {
                GameLog.Error($"ResMgr load callback failed. key: {key}", e, "Res");
            }
        }

        public int Priority { get; }
        public void OnInit()
        {
            throw new NotImplementedException();
        }

        public void OnUpdate(float deltaTime)
        {
            throw new NotImplementedException();
        }

        public void OnShutdown()
        {
            throw new NotImplementedException();
        }
    }
}
