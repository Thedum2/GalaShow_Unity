using UnityCommunity.UnitySingleton;

namespace Galashow.Bridge
{
    public class MainHandler : PersistentMonoSingleton<MainHandler>
    {
        private SampleManager _sampleManager;

        public void Initialize()
        {
            _sampleManager = new SampleManager();
        }
        
        public override void InitializeSingleton()
        {
            base.InitializeSingleton();
            Initialize();
            BridgeManager.Instance.RegisterHandler(new SampleManager());
        }

        public void HelloWorld(string message)
        {
            var notificationData = new
            {
                message = message
            };
            
            BridgeManager.Instance.SendNotify("SampleManager",notificationData);
        }
    }
}