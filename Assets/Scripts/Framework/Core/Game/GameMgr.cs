using System;
using System.Collections.Generic;
using Scripts.Framework.Core.Log;
using UnityEngine;

namespace Scripts.Framework.Core.Game
{
    /// <summary>
    /// 游戏总入口：负责框架启动、Manager 注册/驱动、退出时的统一清理。
    /// 使用：启动场景调用 GameMgr.StartUp()（或挂 GameLauncher），
    /// 之后通过 RegisterManager 注册各 Manager（如 ModuleMgr、PanelMgr、InputMgr）。
    /// </summary>
    public class GameMgr : MonoSingleton<GameMgr>
    {
        protected override bool IsDontDestroyOnload() => true;

        private readonly List<IManager> m_managers = new List<IManager>();
        private bool m_bStarted;
        private bool m_bShuttingDown;

        public static bool IsStarted => Exists && Inst.m_bStarted;

        public static void StartUp()
        {
            var inst = Inst;
            if (inst == null || inst.m_bStarted)
            {
                return;
            }

            inst.m_bStarted = true;
            inst.m_bShuttingDown = false;

            GameLog.Init(true, LogLevel.Debug, () => Time.frameCount);
            GameLog.Info("GameMgr StartUp", "Game");
            
            // 开始加载各个manager，按优先级顺序注册
        }

        #region Manager 管理
        /// <summary>
        /// 注册 Manager。StartUp 之后注册的 Manager 会立即 OnInit。
        /// </summary>
        public static void RegisterManager(IManager manager)
        {
            if (manager == null)
            {
                return;
            }

            var inst = Inst;
            if (inst == null || inst.m_bShuttingDown)
            {
                return;
            }

            if (inst.m_managers.Contains(manager))
            {
                GameLog.Warning($"Manager already registered: {manager.GetType().Name}", "Game");
                return;
            }

            // 按 Priority 升序插入，保持 Update/Shutdown 顺序稳定
            int index = inst.m_managers.Count;
            for (int i = 0; i < inst.m_managers.Count; i++)
            {
                if (manager.Priority < inst.m_managers[i].Priority)
                {
                    index = i;
                    break;
                }
            }

            inst.m_managers.Insert(index, manager);

            if (inst.m_bStarted)
            {
                SafeCall(manager.OnInit, manager, "OnInit");
            }
        }

        public static T GetManager<T>() where T : class, IManager
        {
            if (!Exists)
            {
                return null;
            }

            var managers = Inst.m_managers;
            foreach (var mgr in managers)
            {
                if (mgr is T manager)
                {
                    return manager;
                }
            }

            return null;
        }
        #endregion

        /// <summary>
        /// 统一关闭：反序关闭 Manager -> 取消所有计时 -> 清空事件监听 -> 销毁所有纯 C# 单例。
        /// 关闭后允许再次 StartUp（应对 Editor 关闭 Domain Reload 的情况）。
        /// </summary>
        public static void Shutdown()
        {
            if (!Exists)
            {
                return;
            }

            var inst = Inst;
            if (inst == null || !inst.m_bStarted || inst.m_bShuttingDown)
            {
                return;
            }

            inst.m_bShuttingDown = true;
            GameLog.Info("GameMgr Shutdown", "Game");

            // 管理器清理 反序关闭各 Manager
            for (int i = inst.m_managers.Count - 1; i >= 0; i--)
            {
                SafeCall(inst.m_managers[i].OnShutdown, inst.m_managers[i], "OnShutdown");
            }

            inst.m_managers.Clear(); 
            Singleton.DisposeAll(); // 单例移除所有
            DisposeAll(); // mono单例移除所有
            GameLog.Clear(); // 最后的日志清理

            inst.m_bStarted = false;
            inst.m_bShuttingDown = false;
        }

        private void Update()
        {
            if (!m_bStarted || m_bShuttingDown)
            {
                return;
            }

            float deltaTime = Time.deltaTime;
            // 快照长度：OnUpdate 中注册的新 Manager 下一帧才参与驱动
            int count = m_managers.Count;
            for (int i = 0; i < count; i++)
            {
                // 不用 SafeCall：每帧每 Manager 构造闭包会产生 GC 分配
                try
                {
                    m_managers[i].OnUpdate(deltaTime);
                }
                catch (Exception e)
                {
                    GameLog.Error($"Manager {m_managers[i].GetType().Name}.OnUpdate failed", e, "Game");
                }
            }
        }

        private static void SafeCall(Action action, IManager manager, string phase)
        {
            try
            {
                action?.Invoke();
            }
            catch (Exception e)
            {
                GameLog.Error($"Manager {manager.GetType().Name}.{phase} failed", e, "Game");
            }
        }

        protected void OnApplicationQuit()
        {
            Shutdown();
        }
    }
}
