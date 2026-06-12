namespace Scripts.Framework.Core.Game
{
    public interface IManager
    {
        /// <summary>
        /// 优先级：越小越先 Init / Update，Shutdown 时反序
        /// </summary>
        int Priority { get; }

        void OnInit();

        void OnUpdate(float deltaTime);

        void OnShutdown();
    }
}
