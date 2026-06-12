using UnityEngine.ResourceManagement.AsyncOperations;

namespace Scripts.Framework.Core.Res
{
    /// <summary>
    /// 资源句柄：包一层 AsyncOperationHandle，记录 key/owner，
    /// 业务侧只持有 ResHandle，不直接依赖 Addressables API。
    /// 释放必须走 ResMgr.Release / ReleaseAll。
    /// </summary>
    public sealed class ResHandle
    {
        public string Key { get; }
        public object Owner { get; }

        /// <summary>是否是 InstantiateAsync 创建的实例（释放时走 ReleaseInstance 销毁 GameObject）</summary>
        public bool IsInstance { get; }

        public bool IsReleased { get; internal set; }

        internal AsyncOperationHandle Handle;

        internal ResHandle(string key, object owner, bool isInstance, AsyncOperationHandle handle)
        {
            Key = key;
            Owner = owner;
            IsInstance = isInstance;
            Handle = handle;
        }

        public bool IsDone => IsReleased || Handle.IsDone;

        public bool IsSucceeded => !IsReleased && Handle.Status == AsyncOperationStatus.Succeeded;

        public float Progress => IsReleased ? 0f : Handle.PercentComplete;

        /// <summary>
        /// 加载完成后获取资源。未完成或已释放返回 null。
        /// </summary>
        public T GetAsset<T>() where T : class
        {
            if (IsReleased || !Handle.IsValid() || Handle.Status != AsyncOperationStatus.Succeeded)
            {
                return null;
            }

            return Handle.Result as T;
        }
    }
}
