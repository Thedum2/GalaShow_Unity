using Galashow.Core;
using UnityEngine;

namespace Galashow.Bridge
{
    public class MainHandler
    {
        private SimulationHandler SimulationHandler { get; set; }
        private GameHandler GameHandler { get; set; }

        public void Initialize()
        {
            SimulationHandler = new SimulationHandler();
            GameHandler = new GameHandler();
        }
    }
}