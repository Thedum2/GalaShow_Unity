using UnityCommunity.UnitySingleton;

namespace Galashow.Bridge
{
    public class MainHandler : PersistentMonoSingleton<MainHandler>
    {
        public SampleHandler SampleHandler { get; private set; }

        public void Initialize()
        {
            SampleHandler = new SampleHandler();
        }

        public override void InitializeSingleton()
        {
            base.InitializeSingleton();
            Initialize();
            
            BridgeManager.Instance.RegisterHandler(SampleHandler);
        }
    }
}