using System;
using System.Collections.Generic;

namespace Scripts.Framework.Core
{
    // 非泛型根 — 放共享的静态列表
    public abstract class Singleton
    {
        internal static readonly List<Action> SAllDisposeActions = new();

        public static void DisposeAll()
        {
            // 先拷贝，再清空，再执行
            // DisposeInst 里的 Remove 操作的是已清空的列表，完全无副作用
            var copy = new List<Action>(SAllDisposeActions);
            SAllDisposeActions.Clear();
    
            foreach (var action in copy)
            {
                action?.Invoke();
            }
        }
    }
    
    public abstract class Singleton<T> : Singleton where T : Singleton<T>, new()
    {
        private static T g_inst;
        
        public static bool Exists => g_inst != null;

        public static T Inst
        {
            get
            {
                if (g_inst == null)
                {
                    g_inst = new T();
                    AutoRegisterDispose(DisposeInst);
                }

                return g_inst;
            }
        }
        
        /// <summary>
        /// Unity 的 Play/Stop 不卸载 .NET AppDomain（除非开了 Domain Reload）。C# 的 static 字段存在 AppDomain 里，不是存在 GameObject 上
        /// </summary>
        public static void DisposeInst()
        {
            SAllDisposeActions.Remove(DisposeInst);
            if (g_inst != null)
            {
                g_inst.OnDispose();
                g_inst = null;
            }
        }

        /// <summary>
        /// 实例销毁前的清理钩子：即使外部仍持有旧实例引用，内部状态（如监听器列表）也已清空
        /// </summary>
        protected virtual void OnDispose()
        {
        }

        #region 自动注册-统一销毁
        public static void AutoRegisterDispose(Action disposeAction)
        {
            if (SAllDisposeActions.Contains(disposeAction))
            {
                return;
            }
            
            SAllDisposeActions.Add(disposeAction);
        }
        #endregion
    }
}
