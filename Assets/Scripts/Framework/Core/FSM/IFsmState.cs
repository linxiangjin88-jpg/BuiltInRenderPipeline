namespace Scripts.Framework.Core.FSM
{
    public interface IFsmState<in TOwner>
    {
        /// <summary>
        /// 进入状态时调用
        /// </summary>
        void OnEnter(TOwner owner);

        /// <summary>
        /// 离开状态时调用
        /// </summary>
        void OnExit(TOwner owner);

        /// <summary>
        /// 状态更新时调用
        /// </summary>
        void OnUpdate(TOwner owner, float deltaTime);
    }
}