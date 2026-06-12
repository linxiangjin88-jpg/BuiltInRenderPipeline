using System;
using System.Collections.Generic;
using Scripts.Framework.Core.Log;
using UnityEngine;

namespace Scripts.Framework.Core
{
    /// <summary>
    /// 所有 MonoSingleton 共享的退出标记。
    /// 单独抽成非泛型类，是因为 RuntimeInitializeOnLoadMethod 不能用在泛型类上，
    /// 而关闭 Domain Reload 时 static 字段不会自动重置，必须靠它在每轮 Play 开始时复位。
    /// </summary>
    public abstract class MonoSingleton : MonoBehaviour
    {
        public static bool IsQuitting;
        internal static readonly List<Action> SAllDisposeActions = new();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetOnPlay()
        {
            IsQuitting = false;
        }
        
        public static void DisposeAll()
        {
            IsQuitting = true;
            foreach (var action in SAllDisposeActions)
            {
                action?.Invoke();
            }
        }
    }

    public abstract class MonoSingleton<T> : MonoSingleton where T : MonoSingleton<T>
    {
        private static T g_inst;
        public static bool Exists => g_inst != null;

        public static string CreateName()
        {
            return "[MonoSingleton]" + typeof(T).Name;
        }

        protected virtual bool IsDontDestroyOnload()
        {
            return false;
        }

        public static T Inst
        {
            get
            {
                if (g_inst == null)
                {
                    // 退出阶段（OnApplicationQuit 之后）不再创建实例，
                    // 否则其他对象 OnDestroy 中访问 Inst 会产生场景中残留的"幽灵对象"
                    if (IsQuitting)
                    {
                        GameLog.Error($"[MonoSingleton] Instance '{typeof(T)}' already destroyed on application quit. Won't create again - returning null.");
                        return null;
                    }

                    var go = new GameObject(CreateName());
                    g_inst = go.AddComponent<T>();
                    if (g_inst.IsDontDestroyOnload())
                    {
                        DontDestroyOnLoad(g_inst.gameObject);
                    }

                    AutoRegisterDispose(DisposeInst);
                }

                return g_inst;
            }
        }

        public static void DisposeInst()
        {
            if (!Exists)
            {
                return;
            }
            
            DestroyImmediate(g_inst);
        }
        
        public static void AutoRegisterDispose(Action disposeAction)
        {
            if (SAllDisposeActions.Contains(disposeAction))
            {
                return;
            }
            
            SAllDisposeActions.Add(disposeAction);
        }
        
        protected virtual void OnDestroy()
        {
            if (g_inst == this)
            {
                GameLog.Info($"[MonoSingleton] Instance '{typeof(T)}' destroyed.");
                g_inst = null;   
            }
        }
        
        protected virtual void Awake()
        {
            if (g_inst == null)
            {
                g_inst = this as T;
                if (IsDontDestroyOnload())
                {
                    DontDestroyOnLoad(gameObject);
                }
            }
            else if (g_inst != this)
            {
                Destroy(gameObject);
            }
        }
    }
}
