using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Scripts.Framework.Core.Pool
{
    public class GameObjectPool : IDisposer
    {
        private static Transform g_root; // 所有池的根节点
        private readonly ObjectPool<GameObject> m_pool;
        private Transform m_poolRoot; // 本池的根节点
        public int FreeCount => m_pool.FreeCount;
        public int RentedCount => m_pool.RentedCount;
        public int TotalCount => m_pool.TotalCount;
        
        public GameObjectPool(GameObject prefab, int nInitCount = 0, int nMaxFreeCount = -1)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }
            
            if (g_root == null)
            {
                var rootGo = new GameObject("[GameObjectPool]");
                Object.DontDestroyOnLoad(rootGo);   
                g_root = rootGo.transform;
            }

            var plGo = new GameObject($"[Pool] {prefab.name}");
            plGo.transform.SetParent(g_root, false);
            m_poolRoot = plGo.transform;

            m_pool = new ObjectPool<GameObject>(
                createFunc: () =>
                {
                    var go = Object.Instantiate(prefab, m_poolRoot, false);
                    go.SetActive(false);
                    return go;
                },
                actionReturn: go =>
                {
                    go.SetActive(false);
                    var t = go.transform;
                    t.SetParent(m_poolRoot, false);
                    t.localPosition = Vector3.zero;
                    t.localRotation = Quaternion.identity;
                    t.localScale = Vector3.one;
                },
                actionDestroy: go =>
                {
                    if (go != null)
                    {
                        Object.Destroy(go);
                    }
                },
                nInitCount: nInitCount,
                nMaxFreeCount: nMaxFreeCount
            );
        }
        
        public GameObject Rent()
        {
            EnsureNotDisposed();
            var go = m_pool.Rent();
            go.transform.SetParent(null, false);
            go.SetActive(true);
            return go;
        }
        
        public GameObject Rent(Transform parent, bool worldPositionStays = false)
        {
            EnsureNotDisposed();
            var go = m_pool.Rent();                         
            go.transform.SetParent(parent, worldPositionStays); 
            go.SetActive(true); //  OnEnable：parent 已就绪
            return go;
        }
        
        public GameObject RentWorld(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            EnsureNotDisposed();
            var go = m_pool.Rent();     
            var t = go.transform;
            t.SetParent(parent, false); // 先设置 parent，确保坐标系正确
            t.SetPositionAndRotation(position, rotation); 
            go.SetActive(true);                               
            return go;
        }
        
        public GameObject RentLocal(Vector3 localPosition, Quaternion localRotation, Transform parent = null)
        {
            EnsureNotDisposed();
            var go = m_pool.Rent();    
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localRotation = localRotation;
            go.SetActive(true);
            return go;
        }

        
        public void Return(GameObject go)
        {
            if (go == null)
            {
                return;
            }
            
            // 池已被 Dispose，不再回收：直接销毁并扣减租用计数，避免已销毁对象被压回 m_pool
            if (IsDisposed || m_poolRoot == null)
            {
                m_pool.Discard(go);
                return;
            }
            
            m_pool.Return(go);
        }

        public void Clear()
        {
            if (IsDisposed)
            {
                return;
            }
            
            m_pool.Clear();
        }

        public bool IsDisposed { get; private set; }

        public bool Dispose()
        {
            if (IsDisposed)
            {
                return false;
            }  
            
            IsDisposed = true;
            m_pool.Clear();
            if (m_poolRoot != null)
            {
                Object.Destroy(m_poolRoot.gameObject); // 销毁空容器，避免节点堆积
                m_poolRoot = null;
            }

            return true;
        }
        
        private void EnsureNotDisposed()
        {
            if (IsDisposed || m_poolRoot == null)
            {
                throw new ObjectDisposedException(nameof(GameObjectPool), "Pool 已 Dispose，无法再 Rent。");
            }
        }
    }
}