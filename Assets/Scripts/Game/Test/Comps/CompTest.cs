using Scripts.Framework.Core.Log;
using Scripts.Framework.Core.Timer;
using UnityEngine;

// 测试组件：测试 MonoBehaviour 的生命周期和 TimerMgr 的 CallLater 功能。
namespace Game.Test.Comps
{
    public class CompTest : MonoBehaviour
    {
        void Start()
        {
            GameLog.Info("CompTest Start");
            TimerMgr.Inst.CallLater(  () =>
            {
                GameLog.Info("CompTest TimerMgr.Inst.CallLater Time.frameCount：" + Time.frameCount);
            });
        }
    
        void Update()
        {

        }
        
    }
}