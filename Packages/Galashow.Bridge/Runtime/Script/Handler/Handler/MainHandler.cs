using Galashow.Core;

namespace Galashow.Bridge
{
    public class MainHandler : PersistentMonoSingleton<MainHandler>
    {
        private SimulationHandler SimulationHandler { get; set; }
        private GameHandler GameHandler { get; set; }

        public void Initialize()
        {
            SimulationHandler = new SimulationHandler();
            GameHandler = new GameHandler();
            
            BridgeManager.Instance.RegisterHandler(SimulationHandler);
            BridgeManager.Instance.RegisterHandler(GameHandler);
        }
    }
}