using Galashow.Core;

namespace Galashow.Bridge
{
    public class MainHandler : PersistentMonoSingleton<MainHandler>
    {
        public GameHandler GameHandler { get; private set; }
        public SimulationHandler SimulationHandler { get; private set; }

        public void Initialize()
        {
            GameHandler = new GameHandler();
            SimulationHandler = new SimulationHandler();
            
            BridgeManager.Instance.RegisterHandler(GameHandler);
            BridgeManager.Instance.RegisterHandler(SimulationHandler);
        }

        public override void InitializeSingleton()
        {
            base.InitializeSingleton();
        }
    }
}