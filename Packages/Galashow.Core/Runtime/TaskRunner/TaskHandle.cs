namespace Galashow.Core
{
    public partial interface ITaskHandle { bool IsActive { get; } void Cancel(); }
    public sealed class TaskHandle : ITaskHandle
    {
        internal int Id; internal TaskRunner Runner; internal bool Active = true;
        public bool IsActive => Active;
        public void Cancel() { if (Active) Runner?.Cancel(this); }
    }
}