namespace Scripts.Framework.Core
{
    public interface IDisposer
    {
        bool IsDisposed { get; }
        bool Dispose();
    }
}