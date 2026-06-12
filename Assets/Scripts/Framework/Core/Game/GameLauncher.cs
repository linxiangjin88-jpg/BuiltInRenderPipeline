using UnityEngine;

namespace Scripts.Framework.Core.Game
{
    /// <summary>
    /// 挂在启动场景的入口组件：场景与代码框架的衔接点。
    /// 业务侧可以继承它，在 OnStartUp 里注册自己的模块。
    /// </summary>
    public class GameLauncher : MonoBehaviour
    {
        protected virtual void Awake()
        {
            GameMgr.StartUp();
        }
    }
}
