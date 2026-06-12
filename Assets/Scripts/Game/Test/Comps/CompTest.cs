using Scripts.Framework.Core.Log;
using Scripts.Framework.Core.Timer;
using UnityEngine;

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